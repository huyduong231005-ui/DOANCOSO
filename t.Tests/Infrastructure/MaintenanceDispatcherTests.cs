using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using t.Data;
using t.Infrastructure.Ai;
using t.Infrastructure.Email;
using t.Infrastructure.Maintenance;
using t.Models.Entities;

namespace t.Tests.Infrastructure;

public class MaintenanceDispatcherTests
{
    [Theory]
    [InlineData(MaintenanceCategory.Electrical, "Ổ cắm tóe lửa", SeedData.RoleElectrician)]
    [InlineData(MaintenanceCategory.Plumbing, "Vòi nước rỉ", SeedData.RolePlumber)]
    [InlineData(MaintenanceCategory.Appliance, "Máy lạnh không mát", SeedData.RoleAcTechnician)]
    [InlineData(MaintenanceCategory.Structural, "Cánh tủ bếp sút bản lề", SeedData.RoleCarpenter)]
    [InlineData(MaintenanceCategory.Structural, "Kính cửa sổ phòng khách bị nứt", SeedData.RoleGlazier)]
    [InlineData(MaintenanceCategory.Structural, "cua lua ban cong bi ket ray", SeedData.RoleGlazier)]
    [InlineData(MaintenanceCategory.Pest, "Nhà có gián", null)]
    [InlineData(MaintenanceCategory.Other, "Thẻ từ không quẹt được", null)]
    public void RoleFor_maps_category_to_technician_role(MaintenanceCategory category, string description, string? expected)
    {
        Assert.Equal(expected, MaintenanceDispatcher.RoleFor(category, description));
    }

    [Fact]
    public async Task Routine_request_goes_only_to_the_technician_with_fewest_active_requests()
    {
        await using var db = NewDb();
        var busy = AddUser(db, "busy", SeedData.RoleElectrician);
        var free = AddUser(db, "free", SeedData.RoleElectrician);
        AddUser(db, "manager", SeedData.RoleManager);
        AddUser(db, "admin", SeedData.RoleAdmin);
        db.MaintenanceRequests.Add(new MaintenanceRequest { RequestNumber = "OLD", ApartmentId = 1, AssignedToId = busy.Id });
        var req = AddRequest(db, MaintenanceCategory.Electrical, MaintenancePriority.Medium);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();

        await Dispatcher(db, sender).DispatchAsync(req, null);

        Assert.Equal(free.Id, req.AssignedToId);
        var mail = Assert.Single(sender.Mails);
        Assert.Equal("free@test.local", mail.To);
        Assert.Contains("Giao việc", mail.Subject);
    }

    [Fact]
    public async Task Urgent_request_sends_the_job_to_the_technician_and_a_follow_up_notice_to_managers()
    {
        await using var db = NewDb();
        AddUser(db, "plumber", SeedData.RolePlumber);
        AddUser(db, "manager", SeedData.RoleManager);
        AddUser(db, "admin", SeedData.RoleAdmin);
        var req = AddRequest(db, MaintenanceCategory.Plumbing, MaintenancePriority.Urgent);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();

        await Dispatcher(db, sender).DispatchAsync(req, null);

        Assert.Equal(2, sender.Mails.Count);
        Assert.Contains("KHẨN CẤP - Giao việc", sender.MailTo("plumber@test.local").Subject);
        Assert.Contains("KHẨN CẤP - Cần theo dõi", sender.MailTo("manager@test.local").Subject);
    }

    [Fact]
    public async Task Request_without_a_matching_technician_goes_to_all_managers_in_one_mail()
    {
        await using var db = NewDb();
        AddUser(db, "plumber", SeedData.RolePlumber);
        AddUser(db, "manager", SeedData.RoleManager);
        AddUser(db, "manager2", SeedData.RoleManager);
        AddUser(db, "admin", SeedData.RoleAdmin);
        var req = AddRequest(db, MaintenanceCategory.Pest, MaintenancePriority.Low);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();

        await Dispatcher(db, sender).DispatchAsync(req, null);

        Assert.Null(req.AssignedToId);
        var mail = Assert.Single(sender.Mails);
        Assert.Equal("manager@test.local, manager2@test.local", mail.To);
        Assert.Contains("Cần phân công", mail.Subject);
        Assert.Equal(2, new System.Net.Mail.MailMessage("from@test.local", mail.To).To.Count);
    }

    [Fact]
    public async Task Vague_report_asks_managers_to_follow_up_instead_of_assigning_a_technician()
    {
        await using var db = NewDb();
        AddUser(db, "plumber", SeedData.RolePlumber);
        AddUser(db, "manager", SeedData.RoleManager);
        var req = AddRequest(db, MaintenanceCategory.Plumbing, MaintenancePriority.Medium);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();
        var triage = new MaintenanceTriage(MaintenanceCategory.Plumbing, MaintenancePriority.Medium, false, "{}",
            "Anh/chị cho biết chỗ nào bị chảy nước ạ?");

        await Dispatcher(db, sender).DispatchAsync(req, triage);

        Assert.Null(req.AssignedToId);
        var mail = Assert.Single(sender.Mails);
        Assert.Equal("manager@test.local", mail.To);
        Assert.Contains("Cần hỏi lại cư dân", mail.Subject);
        Assert.Contains("chỗ nào bị chảy nước", System.Net.WebUtility.HtmlDecode(mail.Body));
    }

    [Fact]
    public async Task Admins_receive_manager_mail_only_when_no_manager_exists()
    {
        await using var db = NewDb();
        AddUser(db, "admin", SeedData.RoleAdmin);
        var req = AddRequest(db, MaintenanceCategory.Other, MaintenancePriority.Medium);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();

        await Dispatcher(db, sender).DispatchAsync(req, null);

        Assert.Equal("admin@test.local", Assert.Single(sender.Mails).To);
    }

    [Fact]
    public async Task Admins_get_one_system_alert_when_the_model_is_unavailable()
    {
        await using var db = NewDb();
        AddUser(db, "plumber", SeedData.RolePlumber);
        AddUser(db, "manager", SeedData.RoleManager);
        AddUser(db, "admin", SeedData.RoleAdmin);
        var first = AddRequest(db, MaintenanceCategory.Plumbing, MaintenancePriority.Medium);
        var second = AddRequest(db, MaintenanceCategory.Plumbing, MaintenancePriority.Medium);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();

        await Dispatcher(db, sender).DispatchAsync(first, null, aiUnavailable: true);
        await Dispatcher(db, sender).DispatchAsync(second, null, aiUnavailable: true);

        var alert = Assert.Single(sender.Mails, m => m.To == "admin@test.local");
        Assert.Contains("không phản hồi", alert.Subject);
        Assert.Equal(2, sender.Mails.Count(m => m.To == "plumber@test.local"));
    }

    [Fact]
    public async Task Dispatch_html_encodes_tenant_text_and_does_not_throw_when_sending_fails()
    {
        await using var db = NewDb();
        AddUser(db, "plumber", SeedData.RolePlumber);
        var req = AddRequest(db, MaintenanceCategory.Plumbing, MaintenancePriority.Medium);
        req.Description = "<script>alert(1)</script>";
        await db.SaveChangesAsync();
        var sender = new RecordingSender { Fail = true };

        await Dispatcher(db, sender).DispatchAsync(req, null);

        Assert.NotNull(req.AssignedToId);
        Assert.DoesNotContain("<script>", Assert.Single(sender.Mails).Body);
    }

    [Fact]
    public void Redirect_sends_everything_to_the_demo_inbox_and_keeps_the_original_recipient_in_the_subject()
    {
        Assert.Equal(("demo@test.local", "[Gửi cho tho.dien@luxehaven.vn] Sự cố"),
            SmtpEmailSender.Redirect("demo@test.local", "tho.dien@luxehaven.vn", "Sự cố"));
        Assert.Equal(("tho.dien@luxehaven.vn", "Sự cố"), SmtpEmailSender.Redirect("", "tho.dien@luxehaven.vn", "Sự cố"));
    }

    private static MaintenanceDispatcher Dispatcher(AppDbContext db, IEmailSender sender) =>
        new(db, sender, NullLogger<MaintenanceDispatcher>.Instance);

    private static AppDbContext NewDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"dispatch-{Guid.NewGuid():N}").Options);

    private static AppUser AddUser(AppDbContext db, string name, string roleName)
    {
        var role = db.Roles.Local.FirstOrDefault(r => r.Name == roleName);
        if (role == null)
        {
            role = new IdentityRole(roleName);
            db.Roles.Add(role);
        }
        var user = new AppUser { UserName = name, FullName = name, Email = $"{name}@test.local" };
        db.Users.Add(user);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
        return user;
    }

    private static MaintenanceRequest AddRequest(AppDbContext db, MaintenanceCategory category, MaintenancePriority priority)
    {
        var apartment = new Apartment { Title = "A-0101", Slug = $"a-{Guid.NewGuid():N}", Address = "Test" };
        var req = new MaintenanceRequest
        {
            RequestNumber = "NEW", Apartment = apartment, Title = "Sự cố", Description = "Mô tả",
            Category = category, Priority = priority,
        };
        db.MaintenanceRequests.Add(req);
        return req;
    }

    private sealed class RecordingSender : IEmailSender
    {
        public bool Fail { get; init; }
        public List<(string To, string Subject, string Body)> Mails { get; } = new();

        public (string To, string Subject, string Body) MailTo(string to) => Mails.Single(m => m.To == to);

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            Mails.Add((to, subject, htmlBody));
            return Fail ? Task.FromException(new InvalidOperationException("smtp down")) : Task.CompletedTask;
        }
    }
}
