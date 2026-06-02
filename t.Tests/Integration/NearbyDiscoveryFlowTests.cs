using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using t.Models.ViewModels;

namespace t.Tests.Integration;

public class NearbyDiscoveryFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NearbyDiscoveryFlowTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("/api/rentals/search?latitude=10.7")]
    [InlineData("/api/rentals/search?longitude=106.7")]
    [InlineData("/api/rentals/search?latitude=91&longitude=106.7")]
    [InlineData("/api/rentals/search?latitude=10.7&longitude=181")]
    [InlineData("/api/rentals/search?latitude=NaN&longitude=106.7")]
    [InlineData("/api/rentals/search?latitude=Infinity&longitude=106.7")]
    [InlineData("/api/rentals/search?latitude=abc&longitude=106.7")]
    public async Task RentalsApi_ShouldRejectInvalidCoordinatePairs(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RentalsApi_ShouldReturnLocatedListingsNearestFirst()
    {
        var result = await _client.GetFromJsonAsync<RentalsSearchResultViewModel>(
            "/api/rentals/search?sort=distance_asc&latitude=10.7942&longitude=106.7219&pageSize=50");

        Assert.NotNull(result);
        var distances = result!.Apartments
            .Where(apartment => apartment.DistanceKm.HasValue)
            .Select(apartment => apartment.DistanceKm!.Value)
            .ToList();
        Assert.NotEmpty(distances);
        Assert.Equal(distances.OrderBy(distance => distance), distances);

        var firstUnlocatedIndex = result.Apartments.FindIndex(apartment => !apartment.DistanceKm.HasValue);
        if (firstUnlocatedIndex >= 0)
        {
            Assert.All(
                result.Apartments.Skip(firstUnlocatedIndex),
                apartment => Assert.Null(apartment.DistanceKm));
        }
    }

    [Fact]
    public async Task RentalsPage_ShouldRenderNearbyAction_AndPreserveNearbyState()
    {
        var html = await _client.GetStringAsync(
            "/Home/Rentals?sort=distance_asc&latitude=10.7942&longitude=106.7219");

        Assert.Contains("data-nearby-rentals", html, StringComparison.Ordinal);
        Assert.Contains("name=\"latitude\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"longitude\"", html, StringComparison.Ordinal);
        Assert.Contains("sort=distance_asc", html, StringComparison.Ordinal);
        Assert.Contains("Cách ", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/Home/Rentals?sort=distance_asc&latitude=91&longitude=106.7")]
    [InlineData("/Home/Rentals?sort=distance_asc&latitude=abc&longitude=106.7")]
    public async Task RentalsPage_ShouldIgnoreInvalidNearbyUrl_WithoutServerError(string url)
    {
        var response = await _client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-toast=\"warning\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApartmentDetail_ShouldRenderNearbyRecommendationHeading_AndDistance()
    {
        var html = await _client.GetStringAsync("/Home/ApartmentDetail/1");

        Assert.Contains("Căn hộ gần vị trí này", html, StringComparison.Ordinal);
        Assert.Contains("Cách ", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RentalsNearbyScript_ShouldRequestLocationOnlyInsideClickHandler()
    {
        var script = File.ReadAllText(GetWebRootFile("rentals-nearby.js"));

        Assert.Contains("data-nearby-rentals", script, StringComparison.Ordinal);
        Assert.Contains("addEventListener('click'", script, StringComparison.Ordinal);
        Assert.Contains("navigator.geolocation.getCurrentPosition", script, StringComparison.Ordinal);
        Assert.Contains("searchParams.delete('page')", script, StringComparison.Ordinal);
        Assert.Contains("luxe:page-loaded", script, StringComparison.Ordinal);

        var clickHandlerIndex = script.IndexOf("addEventListener('click'", StringComparison.Ordinal);
        var geolocationIndex = script.IndexOf("navigator.geolocation.getCurrentPosition", StringComparison.Ordinal);
        Assert.True(clickHandlerIndex >= 0 && geolocationIndex > clickHandlerIndex);
    }

    private static string GetWebRootFile(string fileName)
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "t", "wwwroot", "js", fileName));
    }
}
