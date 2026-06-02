using Microsoft.EntityFrameworkCore;
using t.Application.Queries.Rentals;
using t.Data;
using t.Models.Entities;

namespace t.Tests.Integration;

public class NearbyApartmentRecommendationsQueryHandlerTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnNearestActiveApartments_ThenUnlocatedFallback()
    {
        await using var db = CreateDb();
        var seeded = await SeedLocatedScenarioAsync(db);
        var handler = new NearbyApartmentRecommendationsQueryHandler(db);

        var result = await handler.GetAsync(seeded.Current, take: 3);

        Assert.Collection(
            result,
            item => Assert.Equal(seeded.NearId, item.Id),
            item => Assert.Equal(seeded.FarId, item.Id),
            item => Assert.Equal(seeded.UnlocatedId, item.Id));
        Assert.NotNull(result[0].DistanceKm);
        Assert.NotNull(result[1].DistanceKm);
        Assert.Null(result[2].DistanceKm);
        Assert.DoesNotContain(result, item => item.Id == seeded.HiddenCloserId);
    }

    [Fact]
    public async Task GetAsync_ShouldUseSameRegionNewestFirst_WhenCurrentApartmentIsUnlocated()
    {
        await using var db = CreateDb();
        var seeded = await SeedFallbackScenarioAsync(db);
        var handler = new NearbyApartmentRecommendationsQueryHandler(db);

        var result = await handler.GetAsync(seeded.Current, take: 3);

        Assert.Equal(
            new[] { seeded.NewestSameRegionId, seeded.OlderSameRegionId },
            result.Select(item => item.Id));
        Assert.All(result, item => Assert.Null(item.DistanceKm));
        Assert.DoesNotContain(result, item => item.Id == seeded.OtherRegionId);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"nearby-recommendations-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<LocatedScenario> SeedLocatedScenarioAsync(AppDbContext db)
    {
        var seed = await SeedLookupsAsync(db);
        var current = AddApartment(
            db, "current", seed.FirstRegion.Id, seed.Category.Id, seed.Host.Id,
            latitude: 10.7942, longitude: 106.7219, createdAt: new DateTime(2026, 1, 1));
        var near = AddApartment(
            db, "near", seed.SecondRegion.Id, seed.Category.Id, seed.Host.Id,
            latitude: 10.7952, longitude: 106.7193, createdAt: new DateTime(2026, 1, 2));
        var far = AddApartment(
            db, "far", seed.FirstRegion.Id, seed.Category.Id, seed.Host.Id,
            latitude: 10.8530, longitude: 106.7590, createdAt: new DateTime(2026, 1, 3));
        var unlocated = AddApartment(
            db, "unlocated", seed.SecondRegion.Id, seed.Category.Id, seed.Host.Id,
            latitude: null, longitude: null, createdAt: new DateTime(2026, 1, 4));
        var hiddenCloser = AddApartment(
            db, "hidden-closer", seed.FirstRegion.Id, seed.Category.Id, seed.Host.Id,
            latitude: 10.7943, longitude: 106.7218, createdAt: new DateTime(2026, 1, 5),
            status: ListingStatus.Hidden);
        await db.SaveChangesAsync();

        return new LocatedScenario(current, near.Id, far.Id, unlocated.Id, hiddenCloser.Id);
    }

    private static async Task<FallbackScenario> SeedFallbackScenarioAsync(AppDbContext db)
    {
        var seed = await SeedLookupsAsync(db);
        var current = AddApartment(
            db, "current-unlocated", seed.FirstRegion.Id, seed.Category.Id, seed.Host.Id,
            latitude: null, longitude: null, createdAt: new DateTime(2026, 1, 1));
        var olderSameRegion = AddApartment(
            db, "older-same-region", seed.FirstRegion.Id, seed.Category.Id, seed.Host.Id,
            latitude: 10.7, longitude: 106.7, createdAt: new DateTime(2026, 1, 2));
        var newestSameRegion = AddApartment(
            db, "newest-same-region", seed.FirstRegion.Id, seed.Category.Id, seed.Host.Id,
            latitude: 10.8, longitude: 106.8, createdAt: new DateTime(2026, 1, 3));
        var otherRegion = AddApartment(
            db, "other-region", seed.SecondRegion.Id, seed.Category.Id, seed.Host.Id,
            latitude: 10.6, longitude: 106.6, createdAt: new DateTime(2026, 1, 4));
        await db.SaveChangesAsync();

        return new FallbackScenario(current, newestSameRegion.Id, olderSameRegion.Id, otherRegion.Id);
    }

    private static async Task<SeedLookups> SeedLookupsAsync(AppDbContext db)
    {
        var firstRegion = new Region { Name = "First", Slug = "first" };
        var secondRegion = new Region { Name = "Second", Slug = "second" };
        var category = new Category { Name = "Apartment", Slug = "apartment" };
        var host = new AppUser
        {
            Id = "recommendation-host",
            UserName = "recommendation-host@example.com",
            Email = "recommendation-host@example.com",
            FullName = "Recommendation Host"
        };
        db.AddRange(firstRegion, secondRegion, category, host);
        await db.SaveChangesAsync();
        return new SeedLookups(firstRegion, secondRegion, category, host);
    }

    private static Apartment AddApartment(
        AppDbContext db,
        string slug,
        int regionId,
        int categoryId,
        string hostId,
        double? latitude,
        double? longitude,
        DateTime createdAt,
        ListingStatus status = ListingStatus.Active)
    {
        var apartment = new Apartment
        {
            Title = slug,
            Slug = slug,
            Address = $"{slug} address",
            Price = 10_000_000,
            Area = 50,
            Bedrooms = 2,
            Status = status,
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

    private sealed record SeedLookups(
        Region FirstRegion,
        Region SecondRegion,
        Category Category,
        AppUser Host);

    private sealed record LocatedScenario(
        Apartment Current,
        int NearId,
        int FarId,
        int UnlocatedId,
        int HiddenCloserId);

    private sealed record FallbackScenario(
        Apartment Current,
        int NewestSameRegionId,
        int OlderSameRegionId,
        int OtherRegionId);
}
