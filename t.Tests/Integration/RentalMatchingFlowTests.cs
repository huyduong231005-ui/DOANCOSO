using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using t.Application.Queries.Rentals;
using t.Data;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Tests.Integration;

public sealed class RentalMatchingFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RentalMatchingFlowTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterRequiredCriteria_ThenPageByMatchScore()
    {
        await using var db = CreateDb();
        var seeded = await SeedAsync(db);
        var handler = new RentalsQueryHandler(db);

        var result = await handler.SearchAsync(new RentalSearchRequest
        {
            Sort = "match_desc",
            Page = 1,
            PageSize = 1,
            MinBedrooms = 2,
            FurnishingLevel = FurnishingLevel.FullyFurnished,
            RequiredCriteria = ["bedrooms"]
        });

        Assert.Equal(3, result.TotalCount);
        var apartment = Assert.Single(result.Apartments);
        Assert.Equal(seeded.BestId, apartment.Id);
        Assert.Equal(100, apartment.MatchPercent);
        Assert.NotEmpty(apartment.MatchReasons);
    }

    [Fact]
    public async Task SearchAsync_ShouldBreakMatchTiesByCreatedAt_ThenId()
    {
        await using var db = CreateDb();
        var seeded = await SeedAsync(db);
        var handler = new RentalsQueryHandler(db);

        var result = await handler.SearchAsync(new RentalSearchRequest
        {
            Sort = "match_desc",
            MinBedrooms = 2,
            RequiredCriteria = ["bedrooms"],
            PageSize = 10
        });

        Assert.Equal(
            new[] { seeded.NewerTieId, seeded.NewerTieLowerId, seeded.BestId },
            result.Apartments.Select(apartment => apartment.Id));
    }

    [Fact]
    public async Task SearchAsync_ShouldFallbackToNewest_WhenMatchSortHasNoCriteria()
    {
        await using var db = CreateDb();
        var seeded = await SeedAsync(db);
        var handler = new RentalsQueryHandler(db);

        var result = await handler.SearchAsync(new RentalSearchRequest
        {
            Sort = "match_desc",
            PageSize = 10
        });

        Assert.Equal(seeded.RejectedNewestId, result.Apartments[0].Id);
        Assert.All(result.Apartments, apartment => Assert.Equal(100, apartment.MatchPercent));
    }

    [Fact]
    public async Task RentalsApi_ShouldRejectInvalidMatchingQuery()
    {
        var response = await _client.GetAsync(
            "/api/rentals/search?sort=match_desc&requiredCriteria=unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RentalsApi_ShouldReturnMatchMetadata()
    {
        var result = await _client.GetFromJsonAsync<RentalsSearchResultViewModel>(
            "/api/rentals/search?sort=match_desc&minBedrooms=1&pageSize=3");

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Apartments);
        Assert.All(result.Apartments, apartment => Assert.NotNull(apartment.MatchPercent));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"rental-matching-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<SeededMatches> SeedAsync(AppDbContext db)
    {
        var region = new Region { Name = "Selected", Slug = "selected" };
        var category = new Category { Name = "Apartment", Slug = "apartment" };
        var host = new AppUser
        {
            Id = "matching-host",
            UserName = "matching-host@example.com",
            Email = "matching-host@example.com",
            FullName = "Matching Host"
        };
        db.AddRange(region, category, host);
        await db.SaveChangesAsync();

        var best = AddApartment(db, "best", region.Id, category.Id, host.Id,
            bedrooms: 2, furnishing: FurnishingLevel.FullyFurnished, createdAt: new DateTime(2026, 1, 1));
        var newerTieLower = AddApartment(db, "newer-tie-lower", region.Id, category.Id, host.Id,
            bedrooms: 2, furnishing: FurnishingLevel.Basic, createdAt: new DateTime(2026, 1, 2));
        var newerTie = AddApartment(db, "newer-tie", region.Id, category.Id, host.Id,
            bedrooms: 2, furnishing: FurnishingLevel.Basic, createdAt: new DateTime(2026, 1, 2));
        var rejectedNewest = AddApartment(db, "rejected-newest", region.Id, category.Id, host.Id,
            bedrooms: 1, furnishing: FurnishingLevel.None, createdAt: new DateTime(2026, 1, 3));
        await db.SaveChangesAsync();

        return new SeededMatches(best.Id, newerTieLower.Id, newerTie.Id, rejectedNewest.Id);
    }

    private static Apartment AddApartment(
        AppDbContext db,
        string slug,
        int regionId,
        int categoryId,
        string hostId,
        int bedrooms,
        FurnishingLevel furnishing,
        DateTime createdAt)
    {
        var apartment = new Apartment
        {
            Title = slug,
            Slug = slug,
            Address = $"{slug} address",
            Price = 10_000_000,
            Area = 50,
            Bedrooms = bedrooms,
            FurnishingLevel = furnishing,
            AvailableFrom = new DateOnly(2026, 6, 1),
            MinLeaseMonths = 6,
            MaxLeaseMonths = 24,
            Status = ListingStatus.Active,
            HostId = hostId,
            RegionId = regionId,
            CategoryId = categoryId,
            CreatedAt = createdAt
        };
        db.Apartments.Add(apartment);
        return apartment;
    }

    private sealed record SeededMatches(
        int BestId,
        int NewerTieLowerId,
        int NewerTieId,
        int RejectedNewestId);
}
