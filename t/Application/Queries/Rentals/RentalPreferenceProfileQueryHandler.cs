using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Application.Queries.Rentals;

public sealed class RentalPreferenceProfileQueryHandler
{
    private readonly AppDbContext _db;

    public RentalPreferenceProfileQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RentalPreferenceDraft?> GetAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.RentalPreferenceProfiles
            .AsNoTracking()
            .Include(item => item.Categories)
            .Include(item => item.Amenities)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (profile == null)
            return null;

        return new RentalPreferenceDraft
        {
            RegionId = profile.RegionId,
            MinPrice = profile.MinPrice,
            MaxPrice = profile.MaxPrice,
            MinArea = profile.MinArea,
            MaxArea = profile.MaxArea,
            MinBedrooms = profile.MinBedrooms,
            CategoryIds = profile.Categories.Select(item => item.CategoryId).ToHashSet(),
            AmenityIds = profile.Amenities.Select(item => item.AmenityId).ToHashSet(),
            RequiredAmenityIds = profile.Amenities
                .Where(item => item.IsRequired)
                .Select(item => item.AmenityId)
                .ToHashSet(),
            FurnishingLevel = profile.FurnishingLevel,
            AllowsPets = profile.AllowsPets,
            ParkingType = profile.ParkingType,
            AvailableBy = profile.MoveInDate,
            PreferredAddress = profile.PreferredAddress,
            PreferredLatitude = profile.PreferredLatitude,
            PreferredLongitude = profile.PreferredLongitude,
            MaxDistanceKm = profile.MaxDistanceKm,
            MinFloor = profile.MinFloor,
            MaxFloor = profile.MaxFloor,
            HouseDirection = profile.HouseDirection,
            MinLeaseMonths = profile.MinLeaseMonths,
            MaxLeaseMonths = profile.MaxLeaseMonths,
            RequiredCriteria = RequiredCriteria(profile)
        };
    }

    private static IReadOnlySet<string> RequiredCriteria(RentalPreferenceProfile profile)
    {
        var criteria = new HashSet<string>(StringComparer.Ordinal);
        Add(criteria, profile.RequireRegion, "region");
        Add(criteria, profile.RequirePriceRange, "priceRange");
        Add(criteria, profile.RequireAreaRange, "areaRange");
        Add(criteria, profile.RequireBedrooms, "bedrooms");
        Add(criteria, profile.RequireCategoryMatch, "category");
        Add(criteria, profile.RequireMaxDistance, "maxDistance");
        Add(criteria, profile.RequireFurnishing, "furnishing");
        Add(criteria, profile.RequirePets, "pets");
        Add(criteria, profile.RequireParking, "parking");
        Add(criteria, profile.RequireMoveInDate, "moveInDate");
        Add(criteria, profile.RequireFloorRange, "floorRange");
        Add(criteria, profile.RequireDirection, "direction");
        Add(criteria, profile.RequireLeaseRange, "leaseRange");
        return criteria;
    }

    private static void Add(ISet<string> criteria, bool enabled, string key)
    {
        if (enabled)
            criteria.Add(key);
    }
}
