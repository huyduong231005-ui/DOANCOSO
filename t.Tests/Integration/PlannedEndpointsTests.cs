using System.Net;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using t.Data;

namespace t.Tests.Integration;

public class PlannedEndpointsTests : IClassFixture<TestWebApplicationFactory>, IClassFixture<DevelopmentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _devClient;
    private readonly TestWebApplicationFactory _factory;

    public PlannedEndpointsTests(TestWebApplicationFactory factory, DevelopmentWebApplicationFactory devFactory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _devClient = devFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("/du-an")]
    [InlineData("/api/projects")]
    [InlineData("/api/projects/landmark-riverside-collection")]
    [InlineData("/api/rentals/search")]
    [InlineData("/api/auth/email-exists?email=test@example.com")]
    public async Task PlannedEndpoint_ShouldNotReturn404(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DevResetTokenEndpoint_ShouldBeHiddenOutsideDevelopment()
    {
        var response = await _client.GetAsync("/api/dev/auth/reset-token?email=test@example.com");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DevResetTokenEndpoint_ShouldExistInDevelopment()
    {
        var response = await _devClient.GetAsync("/api/dev/auth/reset-token?email=test@example.com");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SeedData_ShouldContainProjects()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains("InMemory", db.Database.ProviderName ?? string.Empty);
        Assert.True(await db.Projects.AnyAsync());
    }

    [Fact]
    public async Task ApartmentDetail_ShouldRenderJavaScriptCoordinatesWithInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("vi-VN");

            var response = await _client.GetAsync("/Home/ApartmentDetail/1");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("data-latitude=\"10.7942\"", html);
            Assert.Contains("data-longitude=\"106.7219\"", html);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task ApartmentDetail_ShouldRenderMapLibreHooks_WithoutProviderCredentials()
    {
        var response = await _client.GetAsync("/Home/ApartmentDetail/1");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("data-mapbox-access-token", html, StringComparison.Ordinal);
        Assert.DoesNotContain("mapbox", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/js/maplibre-loader.js", html, StringComparison.Ordinal);
        Assert.Contains("/js/apartment-detail-map.js", html, StringComparison.Ordinal);
        Assert.DoesNotContain("leaflet", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nominatim", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("google-maps", html, StringComparison.OrdinalIgnoreCase);
    }

}
