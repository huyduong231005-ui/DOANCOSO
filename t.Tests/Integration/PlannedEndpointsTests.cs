using System.Net;
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
}
