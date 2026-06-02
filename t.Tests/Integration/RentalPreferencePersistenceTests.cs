using Microsoft.EntityFrameworkCore;
using t.Application.Commands.RentalPreferences;
using t.Application.Queries.Rentals;
using t.Data;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Tests.Integration;

public sealed class RentalPreferencePersistenceTests
{
    [Fact]
    public async Task SaveAsync_ShouldCreateThenUpdateSingleProfilePerUser()
    {
        await using var db = CreateDb();
        var seeded = await SeedAsync(db);
        var handler = new SaveRentalPreferenceCommandHandler(db);

        await handler.HandleAsync(new SaveRentalPreferenceCommand(
            seeded.UserId,
            Draft(seeded.RegionId, seeded.CategoryId, seeded.WifiId, minPrice: 8_000_000)));
        var changed = Draft(seeded.RegionId, seeded.CategoryId, seeded.WifiId, minPrice: 10_000_000);
        var result = await handler.HandleAsync(new SaveRentalPreferenceCommand(seeded.UserId, changed));

        Assert.True(result.Success);
        Assert.Equal(1, await db.RentalPreferenceProfiles.CountAsync(profile => profile.UserId == seeded.UserId));
        var saved = await db.RentalPreferenceProfiles
            .Include(profile => profile.Categories)
            .Include(profile => profile.Amenities)
            .SingleAsync(profile => profile.UserId == seeded.UserId);
        Assert.Equal(changed.MinPrice, saved.MinPrice);
        Assert.Contains(saved.Amenities, amenity => amenity.AmenityId == seeded.WifiId && amenity.IsRequired);
    }

    [Fact]
    public async Task SaveAsync_ShouldRejectUnknownRelatedIds()
    {
        await using var db = CreateDb();
        var seeded = await SeedAsync(db);
        var handler = new SaveRentalPreferenceCommandHandler(db);
        var draft = Draft(seeded.RegionId, categoryId: 999, seeded.WifiId);

        var result = await handler.HandleAsync(new SaveRentalPreferenceCommand(seeded.UserId, draft));

        Assert.False(result.Success);
        Assert.Empty(await db.RentalPreferenceProfiles.ToListAsync());
    }

    [Fact]
    public async Task SaveAsync_ShouldRejectRequiredAmenityOutsideDesiredAmenities()
    {
        await using var db = CreateDb();
        var seeded = await SeedAsync(db);
        var handler = new SaveRentalPreferenceCommandHandler(db);
        var draft = Draft(seeded.RegionId, seeded.CategoryId, seeded.WifiId);
        draft.RequiredAmenityIds = new HashSet<int> { 999 };

        var result = await handler.HandleAsync(new SaveRentalPreferenceCommand(seeded.UserId, draft));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetAsync_ShouldMapSavedProfileBackToDraft()
    {
        await using var db = CreateDb();
        var seeded = await SeedAsync(db);
        var saveHandler = new SaveRentalPreferenceCommandHandler(db);
        var queryHandler = new RentalPreferenceProfileQueryHandler(db);
        var draft = Draft(seeded.RegionId, seeded.CategoryId, seeded.WifiId, minPrice: 9_000_000);
        await saveHandler.HandleAsync(new SaveRentalPreferenceCommand(seeded.UserId, draft));

        var loaded = await queryHandler.GetAsync(seeded.UserId);

        Assert.NotNull(loaded);
        Assert.Equal(draft.MinPrice, loaded!.MinPrice);
        Assert.Contains(seeded.CategoryId, loaded.CategoryIds);
        Assert.Contains(seeded.WifiId, loaded.RequiredAmenityIds);
        Assert.Contains("priceRange", loaded.RequiredCriteria);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"rental-preferences-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<SeededProfileData> SeedAsync(AppDbContext db)
    {
        var user = new AppUser
        {
            Id = "preference-user",
            UserName = "preference-user@example.com",
            Email = "preference-user@example.com",
            FullName = "Preference User"
        };
        var region = new Region { Name = "Selected", Slug = "selected" };
        var category = new Category { Name = "Apartment", Slug = "apartment" };
        var wifi = new Amenity { Name = "Wi-Fi", Slug = "wifi", Icon = "wifi" };
        db.AddRange(user, region, category, wifi);
        await db.SaveChangesAsync();
        return new SeededProfileData(user.Id, region.Id, category.Id, wifi.Id);
    }

    private static RentalPreferenceDraft Draft(
        int regionId,
        int categoryId,
        int amenityId,
        decimal minPrice = 8_000_000)
    {
        return new RentalPreferenceDraft
        {
            RegionId = regionId,
            MinPrice = minPrice,
            CategoryIds = new HashSet<int> { categoryId },
            AmenityIds = new HashSet<int> { amenityId },
            RequiredAmenityIds = new HashSet<int> { amenityId },
            RequiredCriteria = new HashSet<string> { "region", "priceRange", "category" }
        };
    }

    private sealed record SeededProfileData(
        string UserId,
        int RegionId,
        int CategoryId,
        int WifiId);
}
