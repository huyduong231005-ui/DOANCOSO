using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using t.Data;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Tests.Integration;

public class PlannedFlowTests : IClassFixture<TestWebApplicationFactory>, IClassFixture<DevelopmentWebApplicationFactory>
{
    private static readonly Regex RequestTokenRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly TestWebApplicationFactory _factory;
    private readonly DevelopmentWebApplicationFactory _devFactory;

    public PlannedFlowTests(TestWebApplicationFactory factory, DevelopmentWebApplicationFactory devFactory)
    {
        _factory = factory;
        _devFactory = devFactory;
    }

    [Fact]
    public async Task Register_WithoutInvite_RedirectsToLogin_AndCreatesNoAccount()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = $"new-user-{Guid.NewGuid():N}@example.com";
        var page = await client.GetAsync("/Home/Register");
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.Contains("/Home/Login", page.Headers.Location?.OriginalString);

        var token = await GetRequestVerificationTokenAsync(client, "/Home/Login");
        var post = await client.PostAsync("/Home/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["FullName"] = "Nguoi Dung Moi", ["Email"] = email, ["Password"] = "Abc12345",
            ["ConfirmPassword"] = "Abc12345", ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        using var scope = _factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.AnyAsync(u => u.Email == email));
    }

    [Fact]
    public async Task ForgotAndResetPassword_ShouldWorkWithValidToken_AndFailWithInvalidToken()
    {
        using var client = _devFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        const string email = "renter@luxehaven.vn";

        var forgotToken = await GetRequestVerificationTokenAsync(client, "/Home/ForgotPassword");
        var forgotPost = await client.PostAsync("/Home/ForgotPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["__RequestVerificationToken"] = forgotToken
        }));

        Assert.Equal(HttpStatusCode.OK, forgotPost.StatusCode);

        string validToken;
        using (var scope = _devFactory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
            validToken = await users.GeneratePasswordResetTokenAsync((await users.FindByEmailAsync(email))!);
        }

        var invalidResetPage = await client.GetAsync($"/Home/ResetPassword?email={Uri.EscapeDataString(email)}&token=invalid-token");
        Assert.Equal(HttpStatusCode.OK, invalidResetPage.StatusCode);
        var invalidResetHtml = await invalidResetPage.Content.ReadAsStringAsync();
        var invalidResetRequestToken = ExtractRequestVerificationToken(invalidResetHtml);

        var invalidResetPost = await client.PostAsync("/Home/ResetPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Token"] = "invalid-token",
            ["NewPassword"] = "Host@4567",
            ["ConfirmPassword"] = "Host@4567",
            ["__RequestVerificationToken"] = invalidResetRequestToken
        }));

        Assert.Equal(HttpStatusCode.OK, invalidResetPost.StatusCode);

        var validResetPage = await client.GetAsync($"/Home/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(validToken)}");
        Assert.Equal(HttpStatusCode.OK, validResetPage.StatusCode);
        var validResetHtml = await validResetPage.Content.ReadAsStringAsync();
        var validResetRequestToken = ExtractRequestVerificationToken(validResetHtml);

        var validResetPost = await client.PostAsync("/Home/ResetPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Token"] = validToken,
            ["NewPassword"] = "Host@4567",
            ["ConfirmPassword"] = "Host@4567",
            ["__RequestVerificationToken"] = validResetRequestToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, validResetPost.StatusCode);
        Assert.Equal("/Home/Login", validResetPost.Headers.Location?.OriginalString);

        var loginToken = await GetRequestVerificationTokenAsync(client, "/Home/Login");
        var loginPost = await client.PostAsync("/Home/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "Host@4567",
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = loginToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);
        Assert.Equal("/tenant", loginPost.Headers.Location?.OriginalString);
    }

    [Fact]
    public void SiteCss_ShouldDefineSharedFormFontFamily()
    {
        var cssPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "t", "wwwroot", "css", "site.css"));

        var css = File.ReadAllText(cssPath);
        Assert.Contains("--form-font-family", css, StringComparison.Ordinal);
        Assert.Contains("form input", css, StringComparison.Ordinal);
    }

    private static async Task<string> GetRequestVerificationTokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        return ExtractRequestVerificationToken(html);
    }

    private static string ExtractRequestVerificationToken(string html)
    {
        var match = RequestTokenRegex.Match(html);
        Assert.True(match.Success, "Anti-forgery token was not found in HTML response.");
        return match.Groups[1].Value;
    }
}
