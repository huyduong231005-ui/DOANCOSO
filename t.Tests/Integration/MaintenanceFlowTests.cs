using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using t.Data;
using t.Models.Entities;

namespace t.Tests.Integration;

/// <summary>
/// Quy trình sự cố đầu-cuối qua giao diện: cư dân báo → hệ thống giao thợ đúng chuyên môn → thợ xử lý trên cổng
/// kỹ thuật → cư dân xác nhận và đánh giá. Kèm các luồng bị chặn theo quy tắc nghiệp vụ.
/// </summary>
public sealed class MaintenanceFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Regex TokenRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly TestWebApplicationFactory _factory;

    public MaintenanceFlowTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Resident_Reports_SystemAssignsPlumber_PlumberResolves_ResidentConfirmsWithRating()
    {
        using var resident = await ClientAsync("renter@luxehaven.vn", "Renter@123");
        var id = await ReportAsync(resident, "Vòi nước bếp rỉ nước", "Vòi rửa chén rỉ nước liên tục ở chân vòi.", commonArea: false);

        var r = await LoadAsync(id);
        Assert.Equal(MaintenanceStatus.Acknowledged, r.Status);           // hệ thống đã giao
        Assert.NotNull(r.DueAt);                                          // có hạn SLA
        Assert.Equal("tho.nuoc@luxehaven.vn", await EmailOfAsync(r.AssignedToId));

        using var plumber = await ClientAsync("tho.nuoc@luxehaven.vn", "Tho@123", expectRedirectTo: "/staff/jobs");
        Assert.Contains(r.RequestNumber, await plumber.GetStringAsync("/Staff/Jobs"));

        // Thợ điện không thấy việc của thợ nước (BR-SC-04).
        using var electrician = await ClientAsync("tho.dien@luxehaven.vn", "Tho@123");
        Assert.Equal(HttpStatusCode.NotFound, (await electrician.GetAsync($"/Staff/Jobs/Details/{id}")).StatusCode);

        await PostAsync(plumber, $"/Staff/Jobs/Details/{id}", "/Staff/Jobs/ChangeStatus",
            new() { ["id"] = id.ToString(), ["status"] = ((int)MaintenanceStatus.InProgress).ToString() });
        // Báo hoàn tất mà không ghi kết quả thì bị chặn (BR-SC-06).
        await PostAsync(plumber, $"/Staff/Jobs/Details/{id}", $"/Staff/Jobs/Resolve/{id}", new() { ["resolutionNote"] = " " });
        Assert.Equal(MaintenanceStatus.InProgress, (await LoadAsync(id)).Status);

        await PostAsync(plumber, $"/Staff/Jobs/Details/{id}", $"/Staff/Jobs/Resolve/{id}",
            new() { ["resolutionNote"] = "Thay ron cao su chân vòi", ["photoUrls"] = "/uploads/maintenance/sau-sua.jpg" });
        r = await LoadAsync(id);
        Assert.Equal(MaintenanceStatus.Resolved, r.Status);
        Assert.Contains(r.Photos, p => p.Stage == MaintenancePhotoStage.Result);

        await PostAsync(resident, $"/Tenant/Maintenance/Details/{id}", "/Tenant/Maintenance/ChangeStatus",
            new() { ["id"] = id.ToString(), ["status"] = ((int)MaintenanceStatus.Closed).ToString(), ["rating"] = "5", ["note"] = "Nhanh" });
        r = await LoadAsync(id);
        Assert.Equal(MaintenanceStatus.Closed, r.Status);
        Assert.Equal(5, r.Rating);
        Assert.Equal(
            new MaintenanceStatus?[] { null, MaintenanceStatus.Open, MaintenanceStatus.Acknowledged, MaintenanceStatus.InProgress, MaintenanceStatus.Resolved },
            MaintenanceStatusChange.WithFrom(r.History).Select(x => x.From));
    }

    [Fact]
    public async Task Resident_CannotCancelOnceWorkHasStarted()
    {
        using var resident = await ClientAsync("renter@luxehaven.vn", "Renter@123");
        var id = await ReportAsync(resident, "Ống thoát sàn tắc", "Nước thoát sàn nhà tắm chảy rất chậm.", commonArea: false);
        using var plumber = await ClientAsync("tho.nuoc@luxehaven.vn", "Tho@123");
        await PostAsync(plumber, $"/Staff/Jobs/Details/{id}", "/Staff/Jobs/ChangeStatus",
            new() { ["id"] = id.ToString(), ["status"] = ((int)MaintenanceStatus.InProgress).ToString() });

        await PostAsync(resident, $"/Tenant/Maintenance/Details/{id}", "/Tenant/Maintenance/ChangeStatus",
            new() { ["id"] = id.ToString(), ["status"] = ((int)MaintenanceStatus.Cancelled).ToString(), ["note"] = "Thôi" });

        Assert.Equal(MaintenanceStatus.InProgress, (await LoadAsync(id)).Status);
    }

    [Fact]
    public async Task Resident_ReportsCommonArea_OfOwnBuilding()
    {
        using var resident = await ClientAsync("renter@luxehaven.vn", "Renter@123");
        var id = await ReportAsync(resident, "Đèn hành lang tắt", "Đèn hành lang tầng 12 tắt cả dãy.", commonArea: true, location: "Hành lang tầng 12");

        var r = await LoadAsync(id);
        Assert.Null(r.ApartmentId);
        Assert.Equal("Hành lang tầng 12", r.Location);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ownBuilding = await db.Apartments.Where(a => a.UnitCode == "A-12.05").Select(a => a.Floor.BuildingId).SingleAsync();
        Assert.Equal(ownBuilding, r.CommonBuildingId);
    }

    // ── helpers ──

    private async Task<int> ReportAsync(HttpClient resident, string title, string description, bool commonArea, string? location = null)
    {
        var token = await TokenAsync(resident, "/Tenant/Maintenance/Create");
        var res = await resident.PostAsync("/Tenant/Maintenance/Create", Form(token, new()
        {
            ["Title"] = title, ["Description"] = description,
            ["Category"] = ((int)MaintenanceCategory.Plumbing).ToString(), ["Priority"] = ((int)MaintenancePriority.Medium).ToString(),
            ["CommonArea"] = commonArea ? "true" : "false", ["Location"] = location ?? ""
        }));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        var m = Regex.Match(res.Headers.Location!.OriginalString, @"/Details/(\d+)");
        Assert.True(m.Success, "Không chuyển tới trang chi tiết: " + res.Headers.Location);
        return int.Parse(m.Groups[1].Value);
    }

    private async Task<MaintenanceRequest> LoadAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.MaintenanceRequests.AsNoTracking()
            .Include(x => x.History).Include(x => x.Photos)
            .SingleAsync(x => x.Id == id);
    }

    private async Task<string?> EmailOfAsync(string? userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.Email).SingleOrDefaultAsync();
    }

    private async Task<HttpClient> ClientAsync(string email, string password, string? expectRedirectTo = null)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await TokenAsync(client, "/Home/Login");
        var res = await client.PostAsync("/Home/Login", Form(token, new() { ["Email"] = email, ["Password"] = password, ["RememberMe"] = "false" }));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        if (expectRedirectTo != null) Assert.Equal(expectRedirectTo, res.Headers.Location!.OriginalString);
        return client;
    }

    private static async Task PostAsync(HttpClient client, string pageWithToken, string url, Dictionary<string, string> fields)
    {
        var res = await client.PostAsync(url, Form(await TokenAsync(client, pageWithToken), fields));
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
