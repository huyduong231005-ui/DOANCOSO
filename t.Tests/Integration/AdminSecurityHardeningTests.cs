using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using t.Data;
using t.Models.Entities;

namespace t.Tests.Integration;

public sealed class AdminSecurityHardeningTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Regex RequestTokenRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly TestWebApplicationFactory _factory;

    public AdminSecurityHardeningTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_ShouldRejectAuthenticatedAdminPost_WhenAntiForgeryTokenIsMissing()
    {
        using var client = CreateClient();
        await LoginAsync(client, "admin@luxehaven.vn", "Admin@123", "/admin");

        using var content = new MultipartFormDataContent
        {
            { CreateImageContent(), "files", "csrf-check.jpg" }
        };

        var response = await client.PostAsync("/admin/uploads/multi/apartments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ShouldForbidTenantRole_ForStaffOnlyFolder()
    {
        using var client = CreateClient();
        await LoginAsync(client, "renter@luxehaven.vn", "Renter@123", "/tenant");

        var token = await GetRequestVerificationTokenAsync(client, "/Home/Rentals");
        using var content = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { CreateImageContent(), "files", "tenant-check.jpg" }
        };

        var response = await client.PostAsync("/admin/uploads/multi/apartments", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Home/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Upload_ShouldAllowTenantRole_ForMaintenanceFolder()
    {
        using var client = CreateClient();
        await LoginAsync(client, "renter@luxehaven.vn", "Renter@123", "/tenant");

        var token = await GetRequestVerificationTokenAsync(client, "/Home/Rentals");
        using var content = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { CreateImageContent(), "files", "tenant-maintenance.jpg" }
        };

        var response = await client.PostAsync("/admin/uploads/multi/maintenance", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The tenant is authorized for this folder, so a file is actually written.
        // Remove it so the test leaves no artifact under wwwroot/uploads.
        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);
        using var scope = _factory.Services.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (item.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
            {
                var abs = Path.Combine(env.WebRootPath, url.GetString()!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(abs)) File.Delete(abs);
            }
        }
    }

    [Fact]
    public async Task ApartmentStatusChange_ShouldIgnoreExternalReturnUrl()
    {
        using var client = CreateClient();
        await LoginAsync(client, "admin@luxehaven.vn", "Admin@123", "/admin");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apartmentId = await db.Apartments
            .Where(apartment => apartment.Status == ListingStatus.Active)
            .OrderBy(apartment => apartment.Id)
            .Select(apartment => apartment.Id)
            .FirstAsync();

        var token = await GetRequestVerificationTokenAsync(client, $"/Admin/Apartments/Details/{apartmentId}");
        var response = await client.PostAsync(
            $"/Admin/Apartments/ChangeStatus/{apartmentId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["status"] = ListingStatus.Hidden.ToString(),
                ["returnUrl"] = "https://evil.example/phishing",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Admin/Apartments/Details/{apartmentId}", response.Headers.Location?.OriginalString);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private static async Task LoginAsync(HttpClient client, string email, string password, string expectedRedirect)
    {
        var token = await GetRequestVerificationTokenAsync(client, "/Home/Login");
        var response = await client.PostAsync("/Home/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedRedirect, response.Headers.Location?.OriginalString);
    }

    private static async Task<string> GetRequestVerificationTokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var match = RequestTokenRegex.Match(html);
        Assert.True(match.Success, "Anti-forgery token was not found in HTML response.");
        return match.Groups[1].Value;
    }

    private static ByteArrayContent CreateImageContent()
    {
        var bytes = new byte[1024];
        Random.Shared.NextBytes(bytes);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        return content;
    }
}
