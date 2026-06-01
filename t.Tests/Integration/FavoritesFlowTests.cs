using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using t.Data;

namespace t.Tests.Integration;

public class FavoritesFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly Regex RequestTokenRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly TestWebApplicationFactory _factory;

    public FavoritesFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Set_ShouldSoftDeleteAndRestoreTheSameFavoriteRow()
    {
        var context = await CreateAuthenticatedContextAsync();

        await PostSetAsync(context.Client, context.ApartmentId, shouldBeFavorite: true);

        var inserted = await GetFavoriteRowsAsync(context.UserId, context.ApartmentId);
        var insertedRow = Assert.Single(inserted);
        Assert.False(insertedRow.IsDeleted);

        await PostSetAsync(context.Client, context.ApartmentId, shouldBeFavorite: false);

        var deleted = await GetFavoriteRowsAsync(context.UserId, context.ApartmentId);
        var deletedRow = Assert.Single(deleted);
        Assert.Equal(insertedRow.Id, deletedRow.Id);
        Assert.True(deletedRow.IsDeleted);
        Assert.NotNull(deletedRow.DeletedAt);

        await PostSetAsync(context.Client, context.ApartmentId, shouldBeFavorite: true);

        var restored = await GetFavoriteRowsAsync(context.UserId, context.ApartmentId);
        var restoredRow = Assert.Single(restored);
        Assert.Equal(insertedRow.Id, restoredRow.Id);
        Assert.False(restoredRow.IsDeleted);
        Assert.Null(restoredRow.DeletedAt);
        Assert.Null(restoredRow.DeletedBy);
    }

    [Fact]
    public async Task Set_ShouldBeIdempotent_WhenTheSameDesiredStateIsPostedRepeatedly()
    {
        var context = await CreateAuthenticatedContextAsync();

        await PostSetAsync(context.Client, context.ApartmentId, shouldBeFavorite: true);
        await PostSetAsync(context.Client, context.ApartmentId, shouldBeFavorite: true);
        await PostSetAsync(context.Client, context.ApartmentId, shouldBeFavorite: false);
        await PostSetAsync(context.Client, context.ApartmentId, shouldBeFavorite: false);

        var rows = await GetFavoriteRowsAsync(context.UserId, context.ApartmentId);
        var row = Assert.Single(rows);
        Assert.True(row.IsDeleted);
    }

    [Fact]
    public async Task FavoriteForms_ShouldPostDesiredState_AndExposeSubmitLockHook()
    {
        var context = await CreateAuthenticatedContextAsync();

        var rentalsHtml = await context.Client.GetStringAsync("/Home/Rentals");
        var detailHtml = await context.Client.GetStringAsync($"/Home/ApartmentDetail?id={context.ApartmentId}");

        Assert.Contains("action=\"/Favorites/Set\"", rentalsHtml, StringComparison.Ordinal);
        Assert.Contains("name=\"shouldBeFavorite\"", rentalsHtml, StringComparison.Ordinal);
        Assert.Contains("data-favorite-form", rentalsHtml, StringComparison.Ordinal);

        Assert.Contains("action=\"/Favorites/Set\"", detailHtml, StringComparison.Ordinal);
        Assert.Contains("name=\"shouldBeFavorite\"", detailHtml, StringComparison.Ordinal);
        Assert.Contains("data-favorite-form", detailHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FavoritesPage_ShouldRenderTopbarActiveLinkAndEmptyState()
    {
        var context = await CreateAuthenticatedContextAsync();

        var html = await context.Client.GetStringAsync("/Favorites");

        Assert.Contains("class=\"topbar\"", html, StringComparison.Ordinal);
        Assert.Contains("login-link active", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/Favorites\"", html, StringComparison.Ordinal);
        Assert.Contains("Bạn chưa lưu căn hộ nào", html, StringComparison.Ordinal);
        Assert.Contains("Khám phá tin đăng", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FavoritesPage_ShouldRenderSavedApartmentCards()
    {
        var context = await CreateAuthenticatedContextAsync();
        await PostSetAsync(context.Client, context.ApartmentId, shouldBeFavorite: true);

        var html = await context.Client.GetStringAsync("/Favorites");

        Assert.Contains("listing-card", html, StringComparison.Ordinal);
        Assert.Contains("action=\"/Favorites/Set\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"shouldBeFavorite\" value=\"false\"", html, StringComparison.Ordinal);
        Assert.Contains("data-favorite-form", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Layout_ShouldLoadFavoriteSubmitLockScript()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/Home/Rentals");

        Assert.Contains("/js/favorites.js", html, StringComparison.Ordinal);
    }

    [Fact]
    public void FavoriteSubmitLock_ShouldPreventRepeatedSubmission()
    {
        var scriptPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "t", "wwwroot", "js", "favorites.js"));

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("event.preventDefault()", script, StringComparison.Ordinal);
    }

    private async Task<AuthenticatedContext> CreateAuthenticatedContextAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var email = $"favorite-user-{Guid.NewGuid():N}@example.com";
        var token = await GetRequestVerificationTokenAsync(client, "/Home/Register");
        var registerResponse = await client.PostAsync("/Home/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["FullName"] = "Favorite Test User",
            ["Email"] = email,
            ["Phone"] = "0901234567",
            ["Password"] = "Abc12345",
            ["ConfirmPassword"] = "Abc12345",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.Id)
            .SingleAsync();
        var apartmentId = await db.Apartments
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .FirstAsync();

        return new AuthenticatedContext(client, userId, apartmentId);
    }

    private static async Task PostSetAsync(HttpClient client, int apartmentId, bool shouldBeFavorite)
    {
        var token = await GetRequestVerificationTokenAsync(client, "/Home/Rentals");
        var response = await client.PostAsync("/Favorites/Set", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["apartmentId"] = apartmentId.ToString(),
            ["shouldBeFavorite"] = shouldBeFavorite.ToString(),
            ["returnUrl"] = "/Home/Rentals",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Home/Rentals", response.Headers.Location?.OriginalString);
    }

    private async Task<List<Models.Entities.Favorite>> GetFavoriteRowsAsync(string userId, int apartmentId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Favorites
            .IgnoreQueryFilters()
            .Where(f => f.UserId == userId && f.ApartmentId == apartmentId)
            .ToListAsync();
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

    private sealed record AuthenticatedContext(HttpClient Client, string UserId, int ApartmentId);
}
