using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace t.Tests.Integration;

public sealed class RentalPreferenceUiFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RentalPreferenceUiFlowTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task SaveEndpoint_ShouldRequireLogin()
    {
        var response = await _client.PostAsync(
            "/RentalPreferences/Save",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Home/Login", response.Headers.Location!.OriginalString);
        Assert.Contains("ReturnUrl=", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task RentalsPage_ShouldIgnoreInvalidMatchingQuery_WithoutServerError()
    {
        var response = await _client.GetAsync(
            "/Home/Rentals?sort=match_desc&minLeaseMonths=12&maxLeaseMonths=3");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-toast=\"warning\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RentalsPage_ShouldRenderPreferenceSidebarDrawerAndMatchSort()
    {
        var html = await _client.GetStringAsync("/Home/Rentals?sort=match_desc&minBedrooms=1");

        Assert.Contains("data-rentals-filter-sidebar", html, StringComparison.Ordinal);
        Assert.Contains("data-rentals-results-grid", html, StringComparison.Ordinal);
        Assert.Contains("data-rentals-advanced-drawer", html, StringComparison.Ordinal);
        Assert.Contains("data-rentals-filter-open", html, StringComparison.Ordinal);
        Assert.Contains("data-preference-save", html, StringComparison.Ordinal);
        Assert.Contains("name=\"houseDirection\"", html, StringComparison.Ordinal);
        Assert.Contains("Phù hợp nhất", html, StringComparison.Ordinal);
        Assert.Contains("Bộ lọc nâng cao", html, StringComparison.Ordinal);
        Assert.Contains("Lưu hồ sơ nhu cầu", html, StringComparison.Ordinal);
        Assert.Contains("% phù hợp", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RentalsPreferenceAssets_ShouldSupportResponsiveLayoutAndSoftNavigation()
    {
        var css = File.ReadAllText(GetWebRootFile("css", "rentals-page.css"));
        var script = File.ReadAllText(GetWebRootFile("js", "rentals-preferences.js"));
        var mapHelper = File.ReadAllText(GetWebRootFile("js", "address-map-autocomplete.js"));
        var layout = File.ReadAllText(GetViewFile("Shared", "_Layout.cshtml"));
        var rentalsView = File.ReadAllText(GetViewFile("Home", "Rentals.cshtml"));

        Assert.Contains("rentals-page-shell", css, StringComparison.Ordinal);
        Assert.Contains("270px", css, StringComparison.Ordinal);
        Assert.Contains("z-index: 120", css, StringComparison.Ordinal);
        Assert.Contains("repeat(3, minmax(0, 1fr))", css, StringComparison.Ordinal);
        Assert.DoesNotContain("lg:col-span-", rentalsView, StringComparison.Ordinal);
        Assert.Contains("luxe:page-loaded", script, StringComparison.Ordinal);
        Assert.Contains("data-preference-save", script, StringComparison.Ordinal);
        Assert.Contains("pendingPreferenceSave", script, StringComparison.Ordinal);
        Assert.Contains("window.createLuxeAddressMap", script, StringComparison.Ordinal);
        Assert.Contains("https://photon.komoot.io/api/", mapHelper, StringComparison.Ordinal);
        Assert.Contains("https://tiles.openfreemap.org/styles/liberty", mapHelper, StringComparison.Ordinal);
        Assert.Contains("new AbortController()", mapHelper, StringComparison.Ordinal);
        Assert.Contains("~/js/rentals-preferences.js", layout, StringComparison.Ordinal);
        Assert.True(
            layout.IndexOf("~/js/address-map-autocomplete.js", StringComparison.Ordinal) <
            layout.IndexOf("~/js/post-listing.js", StringComparison.Ordinal));
    }

    private static string GetWebRootFile(params string[] parts)
    {
        return Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "t", "wwwroot", .. parts]));
    }

    private static string GetViewFile(params string[] parts)
    {
        return Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "t", "Views", .. parts]));
    }
}
