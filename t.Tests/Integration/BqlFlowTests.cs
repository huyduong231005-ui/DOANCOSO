using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using t.Data;
using t.Infrastructure.Billing;
using t.Models.Entities;

namespace t.Tests.Integration;

/// <summary>Luồng Ban quản lý: Khu → Toà → Tầng → Căn, cư dân, thu phí theo căn.</summary>
public sealed class BqlFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Regex TokenRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly TestWebApplicationFactory _factory;

    public BqlFlowTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task MonthlyInvoice_ChargesAreaTimesRate_PlusReadingsAndFixedCharges_Once()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gen = scope.ServiceProvider.GetRequiredService<InvoiceGenerator>();

        var (apt, water) = await NewApartmentAsync(db, area: 70);
        var bike = await db.UtilityTypes.SingleAsync(t => t.Code == "XEMAY");
        db.RecurringCharges.Add(new RecurringCharge { ApartmentId = apt.Id, UtilityTypeId = bike.Id, Quantity = 2 });
        var forDate = new DateTime(2031, 3, 1);
        var reading = new UtilityReading
        {
            ApartmentId = apt.Id, UtilityTypeId = water.Id, BillingMonth = 203103,
            PreviousReading = 100, CurrentReading = 110
        };
        db.UtilityReadings.Add(reading);
        await db.SaveChangesAsync();

        var first = await gen.GenerateMonthlyInvoiceAsync(apt.Id, forDate);
        var second = await gen.GenerateMonthlyInvoiceAsync(apt.Id, forDate);

        Assert.True(first.Success, first.Message);
        Assert.False(second.Success);
        var inv = await db.Invoices.Include(i => i.Items).SingleAsync(i => i.Id == first.InvoiceId);
        // Phí quản lý 70 m² × 15.000 (bảng giá), nước 10 m³ × 18.000, gửi xe máy 2 xe × 120.000: mỗi dịch vụ một dòng.
        Assert.Equal(70 * 15_000m + 180_000m + 240_000m, inv.Total);
        Assert.Equal(inv.Total, inv.Balance);
        Assert.Equal(3, inv.Items.Count);
        Assert.Equal(2m, inv.Items.Single(i => i.UtilityTypeId == bike.Id).Quantity);
        Assert.Equal(new DateTime(2031, 3, 8), inv.DueDate);
        Assert.True(await db.Invoices.AnyAsync(UtilityReading.BillingInvoice(apt.Id, 203103)));
    }

    [Fact]
    public async Task MonthlyInvoice_SkipsApartmentNotHandedOver()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gen = scope.ServiceProvider.GetRequiredService<InvoiceGenerator>();
        var (apt, _) = await NewApartmentAsync(db, area: 50, ApartmentCondition.NotHandedOver);

        var result = await gen.GenerateMonthlyInvoiceAsync(apt.Id, new DateTime(2031, 4, 1));

        Assert.False(result.Success);
        Assert.False(await db.Invoices.AnyAsync(i => i.ApartmentId == apt.Id));
    }

    [Fact]
    public async Task AddResident_MarksOccupied_AndMoveOutOfLastResident_MarksVacant()
    {
        int aptId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            aptId = (await NewApartmentAsync(db, 60)).Apartment.Id;
        }
        using var client = await AdminClientAsync();
        var token = await TokenAsync(client, $"/Admin/Apartments/Details/{aptId}");

        var add = await client.PostAsync("/Admin/Apartments/AddResident", Form(token, new()
        {
            ["ApartmentId"] = aptId.ToString(), ["FullName"] = "Nguyễn Văn Test", ["Role"] = "Owner",
            ["MoveInDate"] = "2026-01-01"
        }));
        Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);

        int residentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apt = await db.Apartments.Include(a => a.Residents).ThenInclude(r => r.User).SingleAsync(a => a.Id == aptId);
            Assert.Equal(ApartmentOccupancy.Occupied, apt.Occupancy);
            var r = Assert.Single(apt.Residents);
            Assert.True(r.IsPrimary);
            residentId = r.Id;
        }

        var moveOut = await client.PostAsync("/Admin/Apartments/MoveOut", Form(token, new() { ["residentId"] = residentId.ToString() }));
        Assert.Equal(HttpStatusCode.Redirect, moveOut.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apt = await db.Apartments.Include(a => a.Residents).SingleAsync(a => a.Id == aptId);
            Assert.Equal(ApartmentOccupancy.Vacant, apt.Occupancy);
            Assert.NotNull(apt.Residents.Single().MoveOutDate);
        }
    }

    [Fact]
    public async Task CreateBuilding_GeneratesFloorsAndUnitCodes()
    {
        using var client = await AdminClientAsync();
        int projectId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var p = await db.Projects.OrderBy(x => x.Id).FirstAsync();
            projectId = p.Id;
        }
        var token = await TokenAsync(client, $"/Admin/Buildings/Create?projectId={projectId}");

        var res = await client.PostAsync("/Admin/Buildings/Edit", Form(token, new()
        {
            ["Code"] = "T9", ["ProjectId"] = projectId.ToString(),
            ["Address"] = "x", ["FloorCount"] = "3", ["UnitsPerFloor"] = "4", ["DefaultUnitArea"] = "55",
            ["Status"] = "0", ["DefaultBillingDay"] = "1", ["DefaultLateFeeAfterDays"] = "7", ["DefaultLateFeePercent"] = "2"
        }));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var b = await db2.Buildings.SingleAsync(x => x.Code == "T9");
        var codes = await db2.Apartments.Where(a => a.BuildingId == b.Id).Select(a => a.UnitCode).ToListAsync();
        Assert.Equal(3, await db2.Apartments.Where(a => a.BuildingId == b.Id).Select(a => a.FloorNumber).Distinct().CountAsync());
        Assert.Equal(12, codes.Count);
        Assert.Contains("T9-03.04", codes);

        // Sơ đồ toà vẽ đủ 12 ô căn.
        var html = await client.GetStringAsync($"/Admin/Buildings/Details/{b.Id}");
        Assert.Equal(12, Regex.Matches(html, "class=\"unit ").Count);
    }

    [Fact]
    public async Task BulkReadings_SavesConsumption_AndSkipsDecreasingMeter()
    {
        int buildingId, waterId, a1, a2;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var (apt1, water) = await NewApartmentAsync(db, 50);
            var apt2 = new Apartment { BuildingId = apt1.BuildingId, FloorNumber = apt1.FloorNumber, UnitCode = apt1.UnitCode + "B", Area = 40 };
            db.Apartments.Add(apt2);
            await db.SaveChangesAsync();
            (buildingId, waterId, a1, a2) = (apt1.BuildingId, water.Id, apt1.Id, apt2.Id);
        }
        using var client = await AdminClientAsync();
        var token = await TokenAsync(client, $"/Admin/Utilities/Bulk?buildingId={buildingId}");

        var res = await client.PostAsync("/Admin/Utilities/Bulk", Form(token, new()
        {
            ["BuildingId"] = buildingId.ToString(), ["UtilityTypeId"] = waterId.ToString(), ["BillingMonth"] = "203105", ["Rate"] = "18000",
            ["Rows[0].ApartmentId"] = a1.ToString(), ["Rows[0].PreviousReading"] = "10", ["Rows[0].CurrentReading"] = "22.5",
            ["Rows[1].ApartmentId"] = a2.ToString(), ["Rows[1].PreviousReading"] = "30", ["Rows[1].CurrentReading"] = "5",
        }));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var r = await db2.UtilityReadings.SingleAsync(x => x.ApartmentId == a1 && x.BillingMonth == 203105);
        Assert.Equal(12.5m, r.Consumption);
        Assert.False(await db2.UtilityReadings.AnyAsync(x => x.ApartmentId == a2 && x.BillingMonth == 203105));
    }

    [Theory]
    [InlineData("/Admin/Projects")]
    [InlineData("/Admin/Apartments")]
    [InlineData("/Admin/Residents")]
    [InlineData("/Admin/Utilities/Bulk")]
    [InlineData("/Admin/Dashboard")]
    [InlineData("/Admin/Payments")]
    [InlineData("/Admin/Reports/Occupancy")]
    [InlineData("/Admin/Reports/Finance")]
    [InlineData("/Admin/Reports/Arrears")]
    [InlineData("/Admin/Reports/Maintenance")]
    [InlineData("/Admin/Maintenance?overdue=true")]
    [InlineData("/Admin/Maintenance")]
    [InlineData("/Admin/Invoices")]
    [InlineData("/Admin/Utilities")]
    [InlineData("/Admin/Users")]
    [InlineData("/Admin/AuditLogs")]
    [InlineData("/Admin/AuditLogs?user=Quản")]
    [InlineData("/Admin/Buildings/Suggest?q=A")]
    [InlineData("/")]
    public async Task AdminPages_Render(string path)
    {
        using var client = await AdminClientAsync();
        var res = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // Các trang chi tiết dùng cột đã gọn (tên toà suy ra từ mã, lịch sử không lưu trạng thái trước, tiền tự tính).
    [Fact]
    public async Task DetailAndEditPages_Render()
    {
        int projectId, buildingId, invoiceId, requestId;
        long auditId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            projectId = await db.Projects.Select(x => x.Id).FirstAsync();
            buildingId = await db.Buildings.Select(x => x.Id).FirstAsync();
            invoiceId = await db.Invoices.Where(i => i.Status == InvoiceStatus.Issued).Select(x => x.Id).FirstAsync();
            requestId = await db.MaintenanceRequests.Where(r => r.History.Count > 1).Select(x => x.Id).FirstAsync();
            auditId = await db.AuditLogs.Select(x => x.Id).FirstAsync();
        }
        using var client = await AdminClientAsync();
        foreach (var path in new[]
                 {
                     $"/Admin/Projects/Edit/{projectId}", $"/Admin/Buildings/Edit/{buildingId}",
                     $"/Admin/Invoices/Details/{invoiceId}", $"/Admin/Invoices/EditItems/{invoiceId}",
                     $"/Admin/Maintenance/Details/{requestId}", $"/Admin/AuditLogs/Details/{auditId}"
                 })
        {
            var res = await client.GetAsync(path);
            Assert.True(res.StatusCode == HttpStatusCode.OK, path + " → " + res.StatusCode);
        }
    }

    [Fact]
    public async Task SeededDrilldown_ProjectBuildingPanelAndTenantPortal_Render()
    {
        int projectId, buildingId, aptId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var apt = await db.Apartments.Include(a => a.Building).SingleAsync(a => a.UnitCode == "A-12.05");
            (projectId, buildingId, aptId) = (apt.Building.ProjectId, apt.BuildingId, apt.Id);
        }

        using var admin = await AdminClientAsync();
        Assert.Contains("Toà A", await admin.GetStringAsync($"/Admin/Projects/Details/{projectId}"));
        Assert.Contains("data-drawer-url", await admin.GetStringAsync($"/Admin/Buildings/Details/{buildingId}"));
        var panel = await admin.GetStringAsync($"/Admin/Apartments/Panel/{aptId}");
        Assert.Contains("Căn A-12.05", panel);
        Assert.Contains("Nguyễn Minh Quân", panel);
        Assert.DoesNotContain("<html", panel);
        Assert.Contains("A-12.05", await admin.GetStringAsync($"/Admin/Apartments/Details/{aptId}"));

        using var tenant = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(tenant, "renter@luxehaven.vn", "Renter@123");
        var home = await tenant.GetStringAsync("/Tenant");
        Assert.Contains("A-12.05", home);
        Assert.Equal(HttpStatusCode.OK, (await tenant.GetAsync("/Tenant/Invoices")).StatusCode);
    }

    // ── helpers ──

    private static async Task<(Apartment Apartment, UtilityType Water)> NewApartmentAsync(
        AppDbContext db, decimal area, ApartmentCondition condition = ApartmentCondition.HandedOver)
    {
        var code = "X" + Guid.NewGuid().ToString("N")[..6];
        var project = await db.Projects.FirstAsync();
        var b = new Building
        {
            Code = code, ProjectId = project.Id,
            DefaultBillingDay = 1
        };
        db.Buildings.Add(b);
        await db.SaveChangesAsync();
        await db.SaveChangesAsync();
        var apt = new Apartment { BuildingId = b.Id, FloorNumber = 1, UnitCode = code + "-01.01", Area = area, Bedrooms = 2, Condition = condition };
        db.Apartments.Add(apt);
        await db.SaveChangesAsync();
        return (apt, await db.UtilityTypes.SingleAsync(t => t.Code == "NUOC"));
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, "admin@luxehaven.vn", "Admin@123");
        return client;
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var token = await TokenAsync(client, "/Home/Login");
        var res = await client.PostAsync("/Home/Login", Form(token, new() { ["Email"] = email, ["Password"] = password, ["RememberMe"] = "false" }));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
    }

    private static async Task<string> TokenAsync(HttpClient client, string path)
    {
        var res = await client.GetAsync(path);
        res.EnsureSuccessStatusCode();
        var m = TokenRegex.Match(await res.Content.ReadAsStringAsync());
        Assert.True(m.Success, $"Không thấy anti-forgery token ở {path}");
        return m.Groups[1].Value;
    }

    private static FormUrlEncodedContent Form(string token, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(fields);
    }
}
