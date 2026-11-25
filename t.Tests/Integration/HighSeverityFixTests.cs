using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Email;
using t.Models.Entities;

namespace t.Tests.Integration;

/// <summary>Các lỗi mức cao tìm được khi kiểm thử toàn hệ thống ngày 06/11/2026 (biên bản C1–C9).</summary>
public sealed class HighSeverityFixTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Regex TokenRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly WebApplicationFactory<Program> _factory;
    private readonly List<(string To, string Html)> _mail = new();

    public HighSeverityFixTests(TestWebApplicationFactory factory) =>
        _factory = factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IEmailSender>();
            s.AddSingleton<IEmailSender>(new RecordingSender(_mail));
        }));

    // C1, C2: quyền đọc từ VaiTro_Quyen — quản lý có users.view nhưng không có users.manage, roles.manage.
    [Fact]
    public async Task Manager_CanListUsers_ButCannotEditUsersRolesOrPromoteSelf()
    {
        using var ql = await ClientAsync("quanly@luxehaven.vn", "Quanly@123");
        var managerId = await UserIdAsync("quanly@luxehaven.vn");

        Assert.Equal(HttpStatusCode.OK, (await ql.GetAsync("/Admin/Users")).StatusCode);
        Assert.Contains("/Home/AccessDenied", (await ql.GetAsync($"/Admin/Users/Edit/{managerId}")).Headers.Location?.OriginalString);
        Assert.Contains("/Home/AccessDenied", (await ql.GetAsync("/Admin/Roles")).Headers.Location?.OriginalString);

        var token = await TokenAsync(ql, "/Admin/Users");
        var promote = await ql.PostAsync("/Admin/Users/Edit", Form(token, new()
        {
            ["Id"] = managerId, ["Email"] = "quanly@luxehaven.vn", ["FullName"] = "QL",
            ["SelectedRoles"] = "Admin"
        }));
        Assert.Contains("/Home/AccessDenied", promote.Headers.Location?.OriginalString);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.False(await users.IsInRoleAsync((await users.FindByIdAsync(managerId))!, "Admin"));
    }

    [Fact]
    public async Task Admin_CannotLockOwnAccount()
    {
        using var admin = await ClientAsync("admin@luxehaven.vn", "Admin@123");
        var adminId = await UserIdAsync("admin@luxehaven.vn");
        var token = await TokenAsync(admin, "/Admin/Users");

        await admin.PostAsync($"/Admin/Users/ToggleLock/{adminId}", Form(token, new()));

        using var scope = _factory.Services.CreateScope();
        var u = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByIdAsync(adminId);
        Assert.True(u!.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow);
    }

    // C3: nhật ký không được giữ mật khẩu đã băm.
    [Fact]
    public async Task AuditLog_DoesNotStorePasswordHash()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var u = new AppUser { UserName = $"au{Guid.NewGuid():N}"[..12] + "@example.com", FullName = "Audit" }; // email hợp lệ (RequireUniqueEmail)
        u.Email = u.UserName;
        Assert.True((await users.CreateAsync(u, "Audit@123")).Succeeded);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var logs = await db.AuditLogs.Where(l => l.EntityName == nameof(AppUser) && l.EntityKey == u.Id).ToListAsync();

        Assert.NotEmpty(logs);
        Assert.All(logs, l => Assert.DoesNotContain(u.PasswordHash!, (l.NewValues ?? "") + (l.OldValues ?? "")));
    }

    // C4: thêm cư dân với email đã có tài khoản thì chưa hiệu lực, chỉ gửi thư mời để xác nhận (BR-CD-02).
    [Fact]
    public async Task AddResident_WithExistingAccountEmail_NotConfirmedUntilInvite()
    {
        int aptId;
        using (var scope = _factory.Services.CreateScope())
            aptId = (await NewApartmentAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>())).Id;
        using var admin = await ClientAsync("admin@luxehaven.vn", "Admin@123");
        var token = await TokenAsync(admin, $"/Admin/Apartments/Details/{aptId}");

        await admin.PostAsync("/Admin/Apartments/AddResident", Form(token, new()
        {
            ["ApartmentId"] = aptId.ToString(), ["FullName"] = "Trùng email", ["Role"] = "Owner",
            ["Email"] = "renter3@luxehaven.vn", ["MoveInDate"] = "2026-01-01"
        }));

        using var check = _factory.Services.CreateScope();
        var r = await check.ServiceProvider.GetRequiredService<AppDbContext>().ApartmentResidents.SingleAsync(x => x.ApartmentId == aptId);
        // Một người một tài khoản: dùng lại tài khoản đã có, nhưng chưa xác nhận thì cổng cư dân chưa hiện căn.
        Assert.Equal("renter3@luxehaven.vn", r.User.Email);
        Assert.Null(r.ConfirmedAt);
        Assert.Contains(_mail, m => m.To == "renter3@luxehaven.vn");
    }

    // C5: sai mật khẩu 5 lần thì khoá, lần đúng ngay sau đó cũng bị từ chối.
    [Fact]
    public async Task Login_LocksAccountAfterFiveFailures()
    {
        var email = $"lk{Guid.NewGuid():N}"[..12] + "@example.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            Assert.True((await users.CreateAsync(new AppUser { UserName = email, Email = email, FullName = "Khoá" }, "Dung@123")).Succeeded);
        }
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        for (var i = 0; i < 5; i++)
            await client.PostAsync("/Home/Login", Form(await TokenAsync(client, "/Home/Login"),
                new() { ["Email"] = email, ["Password"] = $"Sai{i}@123", ["RememberMe"] = "false" }));

        var right = await client.PostAsync("/Home/Login", Form(await TokenAsync(client, "/Home/Login"),
            new() { ["Email"] = email, ["Password"] = "Dung@123", ["RememberMe"] = "false" }));

        Assert.Equal(HttpStatusCode.OK, right.StatusCode);
        Assert.Contains("đang bị khoá", await right.Content.ReadAsStringAsync());
    }

    // T8 + C5: quên mật khẩu gửi liên kết đặt lại qua email, không còn API lộ mã.
    [Fact]
    public async Task ForgotPassword_EmailsResetLink()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.PostAsync("/Home/ForgotPassword", Form(await TokenAsync(client, "/Home/ForgotPassword"),
            new() { ["Email"] = "renter4@luxehaven.vn" }));

        Assert.Contains(_mail, m => m.To == "renter4@luxehaven.vn" && m.Html.Contains("/Home/ResetPassword"));
    }

    // C6, C7: bỏ dòng thì tổng giảm theo; không cho giảm tổng xuống dưới số đã thu.
    [Fact]
    public async Task EditItems_RemovedLineLeavesTotal_AndCannotDropBelowPaid()
    {
        int invId; int[] itemIds;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apt = await NewApartmentAsync(db);
            var res = await scope.ServiceProvider.GetRequiredService<InvoiceGenerator>().GenerateOneOffInvoiceAsync(
                apt.Id, "Thử", new[] { ("Phí A", 1m, 100_000m), ("Phí B", 1m, 120_000m) }, DateTime.Today.AddDays(7));
            invId = res.InvoiceId!.Value;
            itemIds = await db.InvoiceItems.Where(i => i.InvoiceId == invId).OrderBy(i => i.Id).Select(i => i.Id).ToArrayAsync();
        }
        using var admin = await ClientAsync("admin@luxehaven.vn", "Admin@123");
        var page = $"/Admin/Invoices/Details/{invId}";

        await admin.PostAsync("/Admin/Invoices/EditItems", Form(await TokenAsync(admin, page), new()
        {
            ["InvoiceId"] = invId.ToString(), ["Discount"] = "0", ["Tax"] = "0",
            ["Lines[0].Id"] = itemIds[0].ToString(), ["Lines[0].Description"] = "Phí A",
            ["Lines[0].Quantity"] = "1", ["Lines[0].UnitPrice"] = "100000"
        }));
        Assert.Equal(100_000m, await TotalAsync(invId));

        await admin.PostAsync("/Admin/Invoices/RecordPayment", Form(await TokenAsync(admin, page), new()
        {
            ["InvoiceId"] = invId.ToString(), ["Amount"] = "80000", ["Method"] = "0"
        }));
        await admin.PostAsync("/Admin/Invoices/EditItems", Form(await TokenAsync(admin, page), new()
        {
            ["InvoiceId"] = invId.ToString(), ["Discount"] = "0", ["Tax"] = "0",
            ["Lines[0].Id"] = itemIds[0].ToString(), ["Lines[0].Description"] = "Phí A",
            ["Lines[0].Quantity"] = "1", ["Lines[0].UnitPrice"] = "50000"
        }));
        Assert.Equal(100_000m, await TotalAsync(invId));
    }

    // T1: đã hoàn một phần vẫn hoàn tiếp được phần còn lại.
    [Fact]
    public async Task Refund_CanContinueAfterPartialRefund()
    {
        int invId;
        using (var scope = _factory.Services.CreateScope())
        {
            var apt = await NewApartmentAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
            invId = (await scope.ServiceProvider.GetRequiredService<InvoiceGenerator>().GenerateOneOffInvoiceAsync(
                apt.Id, "Thử", new[] { ("Phí", 1m, 300_000m) }, DateTime.Today.AddDays(7))).InvoiceId!.Value;
        }
        using var admin = await ClientAsync("admin@luxehaven.vn", "Admin@123");
        await admin.PostAsync("/Admin/Invoices/RecordPayment", Form(await TokenAsync(admin, $"/Admin/Invoices/Details/{invId}"),
            new() { ["InvoiceId"] = invId.ToString(), ["Amount"] = "300000", ["Method"] = "0" }));
        int payId;
        using (var scope = _factory.Services.CreateScope())
            payId = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Payments.SingleAsync(p => p.InvoiceId == invId)).Id;

        await admin.PostAsync($"/Admin/Payments/Refund/{payId}", Form(await TokenAsync(admin, "/Admin/Payments"), new() { ["amount"] = "100000" }));
        await admin.PostAsync($"/Admin/Payments/Refund/{payId}", Form(await TokenAsync(admin, "/Admin/Payments"), new()));

        using var check = _factory.Services.CreateScope();
        var p = await check.ServiceProvider.GetRequiredService<AppDbContext>().Payments.SingleAsync(x => x.Id == payId);
        Assert.Equal(PaymentStatus.Refunded, p.Status);
        Assert.Equal(300_000m, p.RefundedAmount);
    }

    // C8, C9: lỡ ngày chốt vẫn được bù trong tháng; hạn = ngày lập + 7.
    [Fact]
    public async Task BillingJob_CatchesUpMissedBillingDay_WithDueSevenDaysAfterIssue()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apt = await NewApartmentAsync(db);
        var asOf = new DateTime(2031, 5, 10); // toà chốt ngày 1, máy chủ chạy lại ngày 10

        await scope.ServiceProvider.GetRequiredService<InvoiceGenerator>().GenerateForBillingDayAsync(asOf);

        var inv = await db.Invoices.SingleAsync(i => i.ApartmentId == apt.Id && i.BillingMonth == 203105);
        Assert.Equal(new DateTime(2031, 5, 17), inv.DueDate);
    }

    [Fact]
    public async Task OneOffInvoice_RejectsDueDateInThePast()
    {
        int aptId;
        using (var scope = _factory.Services.CreateScope())
            aptId = (await NewApartmentAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>())).Id;
        using var admin = await ClientAsync("admin@luxehaven.vn", "Admin@123");

        await admin.PostAsync("/Admin/Invoices/CreateOneOff", Form(await TokenAsync(admin, "/Admin/Invoices/CreateOneOff"), new()
        {
            ["ApartmentId"] = aptId.ToString(), ["Title"] = "Thử", ["DueDate"] = "2020-01-01",
            ["Lines[0].Description"] = "Phí", ["Lines[0].Quantity"] = "1", ["Lines[0].UnitPrice"] = "1000"
        }));

        using var check = _factory.Services.CreateScope();
        Assert.False(await check.ServiceProvider.GetRequiredService<AppDbContext>().Invoices.AnyAsync(i => i.ApartmentId == aptId));
    }

    // T2: chỉ số cho kỳ đã lập hoá đơn (sẽ không bao giờ được tính tiền) và kỳ tháng 13 bị từ chối.
    [Fact]
    public async Task Reading_RejectedForInvoicedPeriod_AndInvalidMonth()
    {
        int aptId; int waterId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            aptId = (await NewApartmentAsync(db)).Id;
            waterId = (await db.UtilityTypes.SingleAsync(t => t.Code == "NUOC")).Id;
            Assert.True((await scope.ServiceProvider.GetRequiredService<InvoiceGenerator>()
                .GenerateMonthlyInvoiceAsync(aptId, new DateTime(2031, 6, 1))).Success);
        }
        using var admin = await ClientAsync("admin@luxehaven.vn", "Admin@123");
        foreach (var month in new[] { "203106", "203113" })
            await admin.PostAsync("/Admin/Utilities/EditReading", Form(await TokenAsync(admin, "/Admin/Utilities/CreateReading"), new()
            {
                ["Id"] = "0", ["ApartmentId"] = aptId.ToString(), ["UtilityTypeId"] = waterId.ToString(), ["BillingMonth"] = month,
                ["PreviousReading"] = "1", ["CurrentReading"] = "5", ["Rate"] = "18000"
            }));

        using var check = _factory.Services.CreateScope();
        Assert.False(await check.ServiceProvider.GetRequiredService<AppDbContext>().UtilityReadings.AnyAsync(r => r.ApartmentId == aptId));
    }

    // T3, T4: căn còn người ở không đổi được sang Chưa bàn giao; không thêm trùng cư dân.
    [Fact]
    public async Task Apartment_CannotBeVacantWithResidents_AndRejectsDuplicateResident()
    {
        int aptId;
        using (var scope = _factory.Services.CreateScope())
            aptId = (await NewApartmentAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>())).Id;
        using var admin = await ClientAsync("admin@luxehaven.vn", "Admin@123");
        var page = $"/Admin/Apartments/Details/{aptId}";
        var resident = new Dictionary<string, string>
        {
            ["ApartmentId"] = aptId.ToString(), ["FullName"] = "Ngô Thử", ["Role"] = "Owner", ["Phone"] = "0900000009", ["MoveInDate"] = "2026-01-01"
        };
        await admin.PostAsync("/Admin/Apartments/AddResident", Form(await TokenAsync(admin, page), new(resident)));
        await admin.PostAsync("/Admin/Apartments/AddResident", Form(await TokenAsync(admin, page), new(resident)));
        await admin.PostAsync($"/Admin/Apartments/ChangeStatus/{aptId}", Form(await TokenAsync(admin, page), new() { ["status"] = "1" }));
        await admin.PostAsync($"/Admin/Apartments/ChangeStatus/{aptId}", Form(await TokenAsync(admin, page), new() { ["status"] = "9" }));

        using var check = _factory.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.ApartmentResidents.CountAsync(r => r.ApartmentId == aptId));
        var apt = await db.Apartments.Include(a => a.Residents).SingleAsync(a => a.Id == aptId);
        Assert.Equal(ApartmentCondition.HandedOver, apt.Condition);
        Assert.Equal(ApartmentOccupancy.Occupied, apt.Occupancy);
    }

    // ── helpers ──

    private async Task<decimal> TotalAsync(int invoiceId)
    {
        using var scope = _factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Invoices.SingleAsync(i => i.Id == invoiceId)).Total;
    }

    private async Task<string> UserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email))!.Id;
    }

    private static async Task<Apartment> NewApartmentAsync(AppDbContext db)
    {
        var code = "H" + Guid.NewGuid().ToString("N")[..6];
        var b = new Building
        {
            Code = code, ProjectId = (await db.Projects.FirstAsync()).Id,
            DefaultBillingDay = 1, ManagementFeePerM2 = 10_000m
        };
        db.Buildings.Add(b);
        await db.SaveChangesAsync();
        var f = new Floor { BuildingId = b.Id, Number = 1 };
        db.Floors.Add(f);
        await db.SaveChangesAsync();
        var apt = new Apartment { FloorId = f.Id, UnitCode = code + "-01.01", Area = 50 };
        db.Apartments.Add(apt);
        await db.SaveChangesAsync();
        return apt;
    }

    private async Task<HttpClient> ClientAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var res = await client.PostAsync("/Home/Login", Form(await TokenAsync(client, "/Home/Login"),
            new() { ["Email"] = email, ["Password"] = password, ["RememberMe"] = "false" }));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        return client;
    }

    private static async Task<string> TokenAsync(HttpClient client, string path)
    {
        var m = TokenRegex.Match(await client.GetStringAsync(path));
        Assert.True(m.Success, $"Không thấy anti-forgery token ở {path}");
        return m.Groups[1].Value;
    }

    private static FormUrlEncodedContent Form(string token, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(fields);
    }

    private sealed class RecordingSender(List<(string To, string Html)> sent) : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            lock (sent) sent.Add((to, htmlBody));
            return Task.CompletedTask;
        }
    }
}
