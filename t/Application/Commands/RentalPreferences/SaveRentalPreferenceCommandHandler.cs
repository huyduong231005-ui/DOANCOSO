using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Application.Commands.RentalPreferences;

public sealed class SaveRentalPreferenceCommandHandler
{
    private readonly AppDbContext _db;

    public SaveRentalPreferenceCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SaveRentalPreferenceResult> HandleAsync(
        SaveRentalPreferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalization = RentalPreferenceNormalizer.Normalize(ToRequest(command.Draft), strict: true);
        if (!normalization.IsValid)
            return new SaveRentalPreferenceResult(false, normalization.Errors);

        var draft = normalization.Draft;
        if (!await _db.Users.AnyAsync(user => user.Id == command.UserId, cancellationToken))
            return SaveRentalPreferenceResult.Invalid("Tài khoản không tồn tại.");
        if (draft.RegionId.HasValue &&
            !await _db.Regions.AnyAsync(region => region.Id == draft.RegionId.Value, cancellationToken))
        {
            return SaveRentalPreferenceResult.Invalid("Khu vực đã chọn không tồn tại.");
        }
        if (!await AllCategoriesExistAsync(draft.CategoryIds, cancellationToken))
            return SaveRentalPreferenceResult.Invalid("Có loại hình nhà không tồn tại.");
        if (!await AllAmenitiesExistAsync(draft.AmenityIds, cancellationToken))
            return SaveRentalPreferenceResult.Invalid("Có tiện ích không tồn tại.");

        var profile = await _db.RentalPreferenceProfiles
            .Include(item => item.Categories)
            .Include(item => item.Amenities)
            .SingleOrDefaultAsync(item => item.UserId == command.UserId, cancellationToken);
        if (profile == null)
        {
            profile = new RentalPreferenceProfile { UserId = command.UserId };
            _db.RentalPreferenceProfiles.Add(profile);
        }
        else
        {
            _db.RentalPreferenceCategories.RemoveRange(profile.Categories);
            _db.RentalPreferenceAmenities.RemoveRange(profile.Amenities);
            profile.Categories.Clear();
            profile.Amenities.Clear();
        }

        ApplyScalars(profile, draft);
        foreach (var categoryId in draft.CategoryIds)
            profile.Categories.Add(new RentalPreferenceCategory { CategoryId = categoryId });
        foreach (var amenityId in draft.AmenityIds)
        {
            profile.Amenities.Add(new RentalPreferenceAmenity
            {
                AmenityId = amenityId,
                IsRequired = draft.RequiredAmenityIds.Contains(amenityId)
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return SaveRentalPreferenceResult.Ok();
    }

    private async Task<bool> AllCategoriesExistAsync(
        IReadOnlySet<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return true;
        return await _db.Categories.CountAsync(category => ids.Contains(category.Id), cancellationToken) == ids.Count;
    }

    private async Task<bool> AllAmenitiesExistAsync(
        IReadOnlySet<int> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return true;
        return await _db.Amenities.CountAsync(amenity => ids.Contains(amenity.Id), cancellationToken) == ids.Count;
    }

    private static RentalSearchRequest ToRequest(RentalPreferenceDraft draft)
    {
        return new RentalSearchRequest
        {
            RegionId = draft.RegionId,
            MinPrice = draft.MinPrice,
            MaxPrice = draft.MaxPrice,
            MinArea = draft.MinArea,
            MaxArea = draft.MaxArea,
            MinBedrooms = draft.MinBedrooms,
            CategoryIds = draft.CategoryIds.ToList(),
            AmenityIds = draft.AmenityIds.ToList(),
            RequiredAmenityIds = draft.RequiredAmenityIds.ToList(),
            FurnishingLevel = draft.FurnishingLevel,
            AllowsPets = draft.AllowsPets,
            ParkingType = draft.ParkingType,
            AvailableBy = draft.AvailableBy,
            PreferredAddress = draft.PreferredAddress,
            PreferredLatitude = draft.PreferredLatitude,
            PreferredLongitude = draft.PreferredLongitude,
            MaxDistanceKm = draft.MaxDistanceKm,
            MinFloor = draft.MinFloor,
            MaxFloor = draft.MaxFloor,
            HouseDirection = draft.HouseDirection,
            MinLeaseMonths = draft.MinLeaseMonths,
            MaxLeaseMonths = draft.MaxLeaseMonths,
            RequiredCriteria = draft.RequiredCriteria.ToList()
        };
    }

    private static void ApplyScalars(
        RentalPreferenceProfile profile,
        RentalPreferenceDraft draft)
    {
        profile.RegionId = draft.RegionId;
        profile.MinPrice = draft.MinPrice;
        profile.MaxPrice = draft.MaxPrice;
        profile.MinArea = draft.MinArea;
        profile.MaxArea = draft.MaxArea;
        profile.MinBedrooms = draft.MinBedrooms;
        profile.PreferredAddress = draft.PreferredAddress;
        profile.PreferredLatitude = draft.PreferredLatitude;
        profile.PreferredLongitude = draft.PreferredLongitude;
        profile.MaxDistanceKm = draft.MaxDistanceKm;
        profile.FurnishingLevel = draft.FurnishingLevel;
        profile.AllowsPets = draft.AllowsPets;
        profile.ParkingType = draft.ParkingType;
        profile.MoveInDate = draft.AvailableBy;
        profile.MinFloor = draft.MinFloor;
        profile.MaxFloor = draft.MaxFloor;
        profile.HouseDirection = draft.HouseDirection;
        profile.MinLeaseMonths = draft.MinLeaseMonths;
        profile.MaxLeaseMonths = draft.MaxLeaseMonths;
        profile.RequireRegion = draft.RequiredCriteria.Contains("region");
        profile.RequirePriceRange = draft.RequiredCriteria.Contains("priceRange");
        profile.RequireAreaRange = draft.RequiredCriteria.Contains("areaRange");
        profile.RequireBedrooms = draft.RequiredCriteria.Contains("bedrooms");
        profile.RequireCategoryMatch = draft.RequiredCriteria.Contains("category");
        profile.RequireMaxDistance = draft.RequiredCriteria.Contains("maxDistance");
        profile.RequireFurnishing = draft.RequiredCriteria.Contains("furnishing");
        profile.RequirePets = draft.RequiredCriteria.Contains("pets");
        profile.RequireParking = draft.RequiredCriteria.Contains("parking");
        profile.RequireMoveInDate = draft.RequiredCriteria.Contains("moveInDate");
        profile.RequireFloorRange = draft.RequiredCriteria.Contains("floorRange");
        profile.RequireDirection = draft.RequiredCriteria.Contains("direction");
        profile.RequireLeaseRange = draft.RequiredCriteria.Contains("leaseRange");
    }
}
