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
    public async Task Dispatch_assigns_the_technician_with_fewest_active_requests_and_emails_them_and_managers()
    {
        await using var db = NewDb();
        var busy = AddUser(db, "busy", SeedData.RoleElectrician);
        var free = AddUser(db, "free", SeedData.RoleElectrician);
        AddUser(db, "manager", SeedData.RoleManager);
        db.MaintenanceRequests.Add(new MaintenanceRequest { RequestNumber = "OLD", ApartmentId = 1, AssignedToId = busy.Id });
        var req = AddRequest(db, MaintenanceCategory.Electrical, MaintenancePriority.Medium);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();

        await new MaintenanceDispatcher(db, sender, NullLogger<MaintenanceDispatcher>.Instance).DispatchAsync(req, null);

        Assert.Equal(free.Id, req.AssignedToId);
        Assert.Equal(new[] { "free@test.local", "manager@test.local" }, sender.Recipients.OrderBy(x => x));
    }

    [Fact]
    public async Task Dispatch_leaves_request_unassigned_and_emails_managers_when_no_technician_covers_the_category()
    {
        await using var db = NewDb();
        AddUser(db, "plumber", SeedData.RolePlumber);
        AddUser(db, "manager", SeedData.RoleManager);
        var req = AddRequest(db, MaintenanceCategory.Pest, MaintenancePriority.Low);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();

        await new MaintenanceDispatcher(db, sender, NullLogger<MaintenanceDispatcher>.Instance).DispatchAsync(req, null);

        Assert.Null(req.AssignedToId);
        Assert.Equal(new[] { "manager@test.local" }, sender.Recipients);
    }

    [Fact]
    public async Task Dispatch_sends_vague_reports_to_managers_instead_of_a_technician()
    {
        await using var db = NewDb();
        AddUser(db, "plumber", SeedData.RolePlumber);
        AddUser(db, "manager", SeedData.RoleManager);
        var req = AddRequest(db, MaintenanceCategory.Plumbing, MaintenancePriority.Medium);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();
        var triage = new MaintenanceTriage(MaintenanceCategory.Plumbing, MaintenancePriority.Medium, false, "{}",
            "Anh/chị cho biết chỗ nào bị chảy nước ạ?");

        await new MaintenanceDispatcher(db, sender, NullLogger<MaintenanceDispatcher>.Instance).DispatchAsync(req, triage);

        Assert.Null(req.AssignedToId);
        Assert.Equal(new[] { "manager@test.local" }, sender.Recipients);
        Assert.Contains("chỗ nào bị chảy nước", System.Net.WebUtility.HtmlDecode(sender.Bodies.Single()));
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

        await new MaintenanceDispatcher(db, sender, NullLogger<MaintenanceDispatcher>.Instance).DispatchAsync(req, null);

        Assert.NotNull(req.AssignedToId);
        Assert.DoesNotContain("<script>", sender.Bodies.Single());
    }

    [Fact]
    public void Redirect_sends_everything_to_the_demo_inbox_and_keeps_the_original_recipient_in_the_subject()
    {
        Assert.Equal(("demo@test.local", "[Gửi cho tho.dien@luxehaven.vn] Sự cố"),
            SmtpEmailSender.Redirect("demo@test.local", "tho.dien@luxehaven.vn", "Sự cố"));
        Assert.Equal(("tho.dien@luxehaven.vn", "Sự cố"), SmtpEmailSender.Redirect("", "tho.dien@luxehaven.vn", "Sự cố"));
    }

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
        public List<string> Recipients { get; } = new();
        public List<string> Bodies { get; } = new();

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            Recipients.Add(to);
            Bodies.Add(htmlBody);
            return Fail ? Task.FromException(new InvalidOperationException("smtp down")) : Task.CompletedTask;
        }
    }
}
