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
    [InlineData("/")]
    [InlineData("/Home/Login")]
    public async Task PlannedEndpoint_ShouldNotReturn404(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Hai API công khai đã bỏ: dò email có tài khoản và lấy mã đặt lại mật khẩu (kể cả ở Development).
    [Theory]
    [InlineData("/api/auth/email-exists?email=test@example.com")]
    [InlineData("/api/dev/auth/reset-token?email=test@example.com")]
    public async Task RemovedAuthApis_ShouldBe404_EvenInDevelopment(string path)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _devClient.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task SeedData_ShouldContainProjects()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_PG"))) // chế độ chạy trên PostgreSQL thì bỏ qua
            Assert.Contains("InMemory", db.Database.ProviderName ?? string.Empty);
        Assert.True(await db.Projects.AnyAsync());
    }
}
