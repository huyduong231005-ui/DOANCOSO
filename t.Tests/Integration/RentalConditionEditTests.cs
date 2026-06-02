using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace t.Tests.Integration;

public sealed class RentalConditionEditTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RentalConditionEditTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task ApartmentDetail_ShouldRenderRentalConditions_InVietnamese()
    {
        var response = await _client.GetAsync("/Home/ApartmentDetail/1");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Điều kiện thuê", html, StringComparison.Ordinal);
        Assert.DoesNotContain("FullyFurnished", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Motorbike", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RentalConditionEditViewsAndControllers_ShouldMapNewFields()
    {
        var hostView = File.ReadAllText(GetProjectFile("Views", "MyListings", "Edit.cshtml"));
        var adminView = File.ReadAllText(GetProjectFile("Areas", "Admin", "Views", "Apartments", "Edit.cshtml"));
        var hostController = File.ReadAllText(GetProjectFile("Controllers", "MyListingsController.cs"));
        var adminController = File.ReadAllText(GetProjectFile("Areas", "Admin", "Controllers", "ApartmentsController.cs"));

        Assert.Contains("Điều kiện thuê", hostView, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"FurnishingLevel\"", hostView, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"MinLeaseMonths\"", hostView, StringComparison.Ordinal);
        Assert.Contains("Điều kiện thuê", adminView, StringComparison.Ordinal);
        Assert.Contains("asp-for=\"ParkingType\"", adminView, StringComparison.Ordinal);
        Assert.Contains("apartment.FurnishingLevel = model.FurnishingLevel", hostController, StringComparison.Ordinal);
        Assert.Contains("apt.FurnishingLevel = input.FurnishingLevel", adminController, StringComparison.Ordinal);
    }

    private static string GetProjectFile(params string[] parts)
    {
        return Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "t", .. parts]));
    }
}
