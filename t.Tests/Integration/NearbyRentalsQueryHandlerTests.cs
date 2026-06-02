using Microsoft.EntityFrameworkCore;
using t.Application.Queries.Rentals;
using t.Data;
using t.Models.Entities;

namespace t.Tests.Integration;

public class NearbyRentalsQueryHandlerTests
{
    [Fact]
    public async Task SearchAsync_ShouldOrderMatchingListingsByDistance_ThenUnlocatedFallback()
    {
        await using var db = CreateDb();
        var seeded = await SeedAsync(db);
        var handler = new RentalsQueryHandler(db);

        var result = await handler.SearchAsync(
            region: seeded.SelectedRegionSlug,
            minPrice: null,
            maxPrice: null,
            minArea: null,
            maxArea: null,
            categoryIds: null,
            amenityIds: null,
            sort: "distance_asc",
            page: 1,
            pageSize: 10,
            latitude: 10.7942,
            longitude: 106.7219);

        Assert.Collection(
            result.Apartments,
            item => Assert.Equal(seeded.NearId, item.Id),
            item => Assert.Equal(seeded.FarId, item.Id),
            item => Assert.Equal(seeded.UnlocatedId, item.Id));
        Assert.NotNull(result.Apartments[0].DistanceKm);
        Assert.NotNull(result.Apartments[1].DistanceKm);
        Assert.Null(result.Apartments[2].DistanceKm);
        Assert.DoesNotContain(result.Apartments, item => item.Id == seeded.ExcludedCloserId);
    }

    [Fact]
    public async Task SearchAsync_ShouldPageAfterDistanceOrdering()
    {
        await using var db = CreateDb();
        var seeded = await SeedAsync(db);
        var handler = new RentalsQueryHandler(db);

        var result = await handler.SearchAsync(
            region: seeded.SelectedRegionSlug,
            minPrice: null,
            maxPrice: null,
            minArea: null,
            maxArea: null,
            categoryIds: null,
            amenityIds: null,
            sort: "distance_asc",
            page: 2,
            pageSize: 1,
            latitude: 10.7942,
            longitude: 106.7219);

        var apartment = Assert.Single(result.Apartments);
        Assert.Equal(seeded.FarId, apartment.Id);
        Assert.NotNull(apartment.DistanceKm);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"nearby-rentals-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<SeededRentals> SeedAsync(AppDbContext db)
    {
        var selectedRegion = new Region { Name = "Selected", Slug = "selected" };
        var excludedRegion = new Region { Name = "Excluded", Slug = "excluded" };
        var category = new Category { Name = "Apartment", Slug = "apartment" };
        var host = new AppUser
        {
            Id = "nearby-host",
            UserName = "nearby-host@example.com",
            Email = "nearby-host@example.com",
            FullName = "Nearby Host"
        };
        db.AddRange(selectedRegion, excludedRegion, category, host);
        await db.SaveChangesAsync();

        var near = AddApartment(
            db, "near", selectedRegion.Id, category.Id, host.Id,
            latitude: 10.7952, longitude: 106.7193, createdAt: new DateTime(2026, 1, 1));
        var far = AddApartment(
            db, "far", selectedRegion.Id, category.Id, host.Id,
            latitude: 10.8530, longitude: 106.7590, createdAt: new DateTime(2026, 1, 2));
        var unlocated = AddApartment(
            db, "unlocated", selectedRegion.Id, category.Id, host.Id,
            latitude: null, longitude: null, createdAt: new DateTime(2026, 1, 3));
        var excludedCloser = AddApartment(
            db, "excluded-closer", excludedRegion.Id, category.Id, host.Id,
            latitude: 10.7943, longitude: 106.7218, createdAt: new DateTime(2026, 1, 4));
        await db.SaveChangesAsync();

        return new SeededRentals(
            selectedRegion.Slug,
            near.Id,
            far.Id,
            unlocated.Id,
            excludedCloser.Id);
    }

    private static Apartment AddApartment(
        AppDbContext db,
        string slug,
        int regionId,
        int categoryId,
        string hostId,
        double? latitude,
        double? longitude,
        DateTime createdAt)
    {
        var apartment = new Apartment
        {
            Title = slug,
            Slug = slug,
            Address = $"{slug} address",
            Price = 10_000_000,
            Area = 50,
            Status = ListingStatus.Active,
            HostId = hostId,
            RegionId = regionId,
            CategoryId = categoryId,
            Latitude = latitude,
            Longitude = longitude,
            CreatedAt = createdAt
        };
        db.Apartments.Add(apartment);
        return apartment;
    }

    private sealed record SeededRentals(
        string SelectedRegionSlug,
        int NearId,
        int FarId,
        int UnlocatedId,
        int ExcludedCloserId);
}
