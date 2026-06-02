using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using t.Data;
using t.Models.Entities;

namespace t.Tests.Integration;

public sealed class SeededRentalConditionsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SeededRentalConditionsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeededApartments_ShouldHaveUsableRentalConditions()
    {
        _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apartments = await db.Apartments.AsNoTracking().ToListAsync();

        Assert.NotEmpty(apartments);
        Assert.All(apartments, apartment =>
        {
            Assert.NotEqual(default, apartment.AvailableFrom);
            Assert.InRange(apartment.MinLeaseMonths, 1, apartment.MaxLeaseMonths);
            Assert.True(Enum.IsDefined(apartment.FurnishingLevel));
            Assert.True(Enum.IsDefined(apartment.ParkingType));
        });

        var centralPark = apartments.Single(x => x.Slug == "vhcp-1pn-view-song");
        Assert.Equal(FurnishingLevel.FullyFurnished, centralPark.FurnishingLevel);
        Assert.Equal(ParkingType.Motorbike, centralPark.ParkingType);
        Assert.Equal(6, centralPark.MinLeaseMonths);
    }
}
