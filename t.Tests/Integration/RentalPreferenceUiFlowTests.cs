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
    public async Task RentalsPage_ShouldRenderCompactQuickFiltersAndGroupedAdvancedFilters()
    {
        var html = await _client.GetStringAsync(
            "/Home/Rentals?region=hcm&minPrice=10000000&maxPrice=20000000&minArea=30&maxArea=80&minBedrooms=2&sort=price_asc");

        Assert.DoesNotContain("Rentals Discovery", html, StringComparison.Ordinal);
        Assert.Contains("data-active-filter-chips", html, StringComparison.Ordinal);
        Assert.Contains("data-active-filter-chip", html, StringComparison.Ordinal);
        Assert.Contains("data-filter-range=\"price\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"minPrice\" type=\"number\" value=\"10000000\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"maxPrice\" type=\"number\" value=\"20000000\"", html, StringComparison.Ordinal);
        Assert.Contains("data-filter-range=\"area\"", html, StringComparison.Ordinal);
        Assert.Contains("data-quick-filter-group=\"amenities\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-quick-filter-group=\"categories\"", html, StringComparison.Ordinal);
        Assert.Contains("data-advanced-filter-group=\"categories\"", html, StringComparison.Ordinal);
        Assert.Contains("data-advanced-filter-group=\"housing\"", html, StringComparison.Ordinal);
        Assert.Contains("data-advanced-filter-group=\"location\"", html, StringComparison.Ordinal);
        Assert.Contains("data-advanced-filter-footer", html, StringComparison.Ordinal);
        Assert.Contains("data-lease-duration-control", html, StringComparison.Ordinal);
        Assert.Contains("data-lease-months-target=\"minLeaseMonths\"", html, StringComparison.Ordinal);
        Assert.Contains("data-lease-months-target=\"maxLeaseMonths\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"preferredLatitude\" value=\"\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"preferredLongitude\" value=\"\"", html, StringComparison.Ordinal);
        Assert.Contains("Địa điểm muốn ở gần", html, StringComparison.Ordinal);
        Assert.Contains("data-preference-secondary-action", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RentalsPage_ShouldUseZeroPriceDefaultsAndHideDeprecatedSampleListing()
    {
        var html = await _client.GetStringAsync("/Home/Rentals");

        Assert.Contains("name=\"minPrice\" type=\"number\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"maxPrice\" type=\"number\"", html, StringComparison.Ordinal);
        Assert.Contains("placeholder=\"0\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"minPrice\" type=\"number\" value=\"0\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"maxPrice\" type=\"number\" value=\"0\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ccm-tan-binh-full-do", File.ReadAllText(GetDataFile("SampleListings.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("Mini Tân Bình full đồ chỉ việc xách vali", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RentalsPage_ShouldTreatZeroPriceRangeAsNoPriceFilter()
    {
        var unfilteredHtml = await _client.GetStringAsync("/Home/Rentals");
        var zeroPriceHtml = await _client.GetStringAsync("/Home/Rentals?minPrice=0&maxPrice=0");

        Assert.Contains("data-rentals-results-grid", zeroPriceHtml, StringComparison.Ordinal);
        Assert.Contains("listing-card", zeroPriceHtml, StringComparison.Ordinal);
        Assert.Equal(
            CountOccurrences(unfilteredHtml, "listing-card"),
            CountOccurrences(zeroPriceHtml, "listing-card"));
        Assert.DoesNotContain("Giá: 0tr - 0tr", zeroPriceHtml, StringComparison.Ordinal);
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
        Assert.Contains("preference-checkbox-label", css, StringComparison.Ordinal);
        Assert.Contains("rentals-active-filters", css, StringComparison.Ordinal);
        Assert.Contains("rentals-filter-range", css, StringComparison.Ordinal);
        Assert.Contains("rentals-advanced-footer", css, StringComparison.Ordinal);
        Assert.Contains("max-height: calc(100svh - 7.5rem)", css, StringComparison.Ordinal);
        Assert.Contains("overscroll-behavior: contain", css, StringComparison.Ordinal);
        Assert.Contains(".rentals-filter-actions button[type=\"submit\"]", css, StringComparison.Ordinal);
        Assert.Contains(".rentals-advanced-footer button[type=\"submit\"]", css, StringComparison.Ordinal);
        Assert.DoesNotContain("lg:col-span-", rentalsView, StringComparison.Ordinal);
        Assert.Contains("luxe:page-loaded", script, StringComparison.Ordinal);
        Assert.Contains("data-preference-save", script, StringComparison.Ordinal);
        Assert.Contains("querySelectorAll('[data-rentals-filter-open]'", script, StringComparison.Ordinal);
        Assert.Contains("syncLeaseDurationControls", script, StringComparison.Ordinal);
        Assert.Contains("data-lease-duration-control", script, StringComparison.Ordinal);
        Assert.Contains("mapElement.hidden = true", mapHelper, StringComparison.Ordinal);
        Assert.Contains("clearPosition()", mapHelper, StringComparison.Ordinal);
        Assert.Contains("resolveAddressBeforeSubmit", mapHelper, StringComparison.Ordinal);
        Assert.Contains("form.addEventListener('submit'", mapHelper, StringComparison.Ordinal);
        Assert.Contains("appendVietnamHint", mapHelper, StringComparison.Ordinal);
        Assert.DoesNotContain("lang', 'vi'", mapHelper, StringComparison.Ordinal);
        Assert.Contains("https://nominatim.openstreetmap.org/search", mapHelper, StringComparison.Ordinal);
        Assert.Contains("fetchNominatimFeatures", mapHelper, StringComparison.Ordinal);
        Assert.Contains("chọn vị trí trên bản đồ", mapHelper, StringComparison.Ordinal);
        Assert.DoesNotContain("setPosition(initialPosition)", mapHelper, StringComparison.Ordinal);
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

    private static string GetDataFile(params string[] parts)
    {
        return Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "t", "Data", .. parts]));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
