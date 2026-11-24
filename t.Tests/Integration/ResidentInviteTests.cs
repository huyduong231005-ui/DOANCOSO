using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using t.Data;
using t.Infrastructure.Email;
using t.Models.Entities;

namespace t.Tests.Integration;

/// <summary>Ban quản lý thêm cư dân trước, cư dân nhận thư mời rồi mới tạo tài khoản.</summary>
public sealed class ResidentInviteTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Regex TokenRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex InviteLinkRegex = new("href=\"([^\"]*/Home/Invite\\?token=[^\"]+)\"", RegexOptions.Compiled);

    private readonly WebApplicationFactory<Program> _factory;
    private readonly RecordingSender _mail = new();

    public ResidentInviteTests(TestWebApplicationFactory factory) =>
        _factory = factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IEmailSender>();
            s.AddSingleton<IEmailSender>(_mail);
        }));

    [Fact]
    public async Task AddResidentWithoutAccount_SendsInvite_RegisteringThroughInviteLinksApartment()
    {
        var email = $"moi{Guid.NewGuid():N}"[..14] + "@example.com";
        var (aptId, unitCode) = await AnyVacantApartmentAsync();

        using var admin = await ClientAsync("admin@luxehaven.vn", "Admin@123");
        var token = await TokenAsync(admin, $"/Admin/Apartments/Details/{aptId}");
        var add = await admin.PostAsync("/Admin/Apartments/AddResident", Form(token, new()
        {
            ["ApartmentId"] = aptId.ToString(), ["FullName"] = "Trần Văn Mới", ["Role"] = nameof(ResidentRole.Tenant),
            ["Email"] = email, ["MoveInDate"] = DateTime.Today.ToString("yyyy-MM-dd")
        }));
        Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);

        var letter = Assert.Single(_mail.Sent, m => m.To == email);
        Assert.Contains(unitCode, letter.Html);
        var link = WebUtility.HtmlDecode(InviteLinkRegex.Match(letter.Html).Groups[1].Value);
        Assert.False(string.IsNullOrEmpty(link), "Thư mời không có liên kết");

        using var guest = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var open = await guest.GetAsync(new Uri(link).PathAndQuery);
        Assert.Equal(HttpStatusCode.Redirect, open.StatusCode);
        var registerUrl = open.Headers.Location!.OriginalString;
        Assert.Contains("/Home/Register", registerUrl);

        var registerPage = await guest.GetStringAsync(registerUrl);
        Assert.Contains(email, registerPage);
        var invite = Regex.Match(registerPage, "name=\"invite\" value=\"([^\"]+)\"").Groups[1].Value;
        var reg = await guest.PostAsync("/Home/Register", Form(TokenRegex.Match(registerPage).Groups[1].Value, new()
        {
            ["FullName"] = "Trần Văn Mới", ["Email"] = email, ["Password"] = "Moi@12345",
            ["ConfirmPassword"] = "Moi@12345", ["invite"] = WebUtility.HtmlDecode(invite)
        }));
        Assert.Equal(HttpStatusCode.Redirect, reg.StatusCode);

        Assert.Contains(unitCode, await guest.GetStringAsync("/Tenant"));
    }

    [Fact]
    public async Task RegisteringWithResidentEmailButNoInvite_DoesNotLinkApartment()
    {
        var email = $"la{Guid.NewGuid():N}"[..14] + "@example.com";
        var (aptId, _) = await AnyVacantApartmentAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ApartmentResidents.Add(new ApartmentResident
            {
                ApartmentId = aptId, User = t.Infrastructure.Residents.PendingAccounts.New("Chủ thật", null, email, null), Role = ResidentRole.Owner, MoveInDate = DateTime.Today
            });
            await db.SaveChangesAsync();
        }

        using var stranger = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await TokenAsync(stranger, "/Home/Login");
        await stranger.PostAsync("/Home/Register", Form(token, new()
        {
            ["FullName"] = "Người lạ", ["Email"] = email, ["Password"] = "La@123456", ["ConfirmPassword"] = "La@123456"
        }));

        using var check = _factory.Services.CreateScope();
        var resident = await check.ServiceProvider.GetRequiredService<AppDbContext>().ApartmentResidents
            .SingleAsync(r => r.User.Email == email);
        Assert.Null(resident.User.PasswordHash); // người lạ không kích hoạt được tài khoản khi không có thư mời
    }

    // ── helpers ──

    private async Task<(int Id, string UnitCode)> AnyVacantApartmentAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apt = await db.Apartments.OrderBy(a => a.Id)
            .FirstAsync(Apartment.IsVacant);
        return (apt.Id, apt.UnitCode);
    }

    private async Task<HttpClient> ClientAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await TokenAsync(client, "/Home/Login");
        var res = await client.PostAsync("/Home/Login", Form(token, new() { ["Email"] = email, ["Password"] = password, ["RememberMe"] = "false" }));
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

    private sealed class RecordingSender : IEmailSender
    {
        public List<(string To, string Subject, string Html)> Sent { get; } = new();

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            lock (Sent) Sent.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }
    }
}
