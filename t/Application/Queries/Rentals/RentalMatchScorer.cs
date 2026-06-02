using t.Infrastructure.Geo;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Application.Queries.Rentals;

public sealed record RentalMatchCandidate(
    int Id,
    DateTime CreatedAt,
    int RegionId,
    decimal Price,
    double Area,
    int Bedrooms,
    int CategoryId,
    IReadOnlySet<int> AmenityIds,
    FurnishingLevel FurnishingLevel,
    bool AllowsPets,
    ParkingType ParkingType,
    DateOnly AvailableFrom,
    int MinLeaseMonths,
    int MaxLeaseMonths,
    int? FloorNumber,
    HouseDirection? HouseDirection,
    double? Latitude,
    double? Longitude);

public sealed record RentalMatchResult(
    bool IsEligible,
    int ScorePercent,
    IReadOnlyList<string> Reasons);

public static class RentalMatchScorer
{
    public static RentalMatchResult Score(
        RentalMatchCandidate candidate,
        RentalPreferenceDraft draft)
    {
        var criteria = BuildCriteria(candidate, draft);
        if (criteria.Any(criterion =>
                draft.RequiredCriteria.Contains(criterion.Key) && !criterion.IsMatch))
        {
            return Rejected();
        }

        if (draft.RequiredAmenityIds.Any(id => !candidate.AmenityIds.Contains(id)))
            return Rejected();

        var amenities = draft.AmenityIds
            .Select(id => new Criterion(
                $"amenity:{id}",
                candidate.AmenityIds.Contains(id),
                "Có tiện ích mong muốn"))
            .ToList();
        var scoredCriteria = criteria.Concat(amenities).ToList();
        var score = scoredCriteria.Count == 0
            ? 100
            : (int)Math.Round(
                scoredCriteria.Count(criterion => criterion.IsMatch) * 100d / scoredCriteria.Count,
                MidpointRounding.AwayFromZero);
        var reasons = scoredCriteria
            .Where(criterion => criterion.IsMatch)
            .Select(criterion => criterion.Reason)
            .Distinct()
            .Take(3)
            .ToList();

        return new RentalMatchResult(true, score, reasons);
    }

    private static List<Criterion> BuildCriteria(
        RentalMatchCandidate candidate,
        RentalPreferenceDraft draft)
    {
        var criteria = new List<Criterion>();

        Add(criteria, draft.MaxDistanceKm.HasValue &&
                      GeoDistance.IsValidCoordinate(draft.PreferredLatitude, draft.PreferredLongitude),
            "maxDistance",
            DistanceMatches(candidate, draft),
            "Gần vị trí ưu tiên");
        Add(criteria, draft.MinPrice.HasValue || draft.MaxPrice.HasValue,
            "priceRange",
            IsWithin(candidate.Price, draft.MinPrice, draft.MaxPrice),
            "Trong khoảng giá mong muốn");
        Add(criteria, draft.RegionId.HasValue,
            "region",
            candidate.RegionId == draft.RegionId,
            "Đúng khu vực mong muốn");
        Add(criteria, draft.CategoryIds.Count > 0,
            "category",
            draft.CategoryIds.Contains(candidate.CategoryId),
            "Đúng loại hình mong muốn");
        Add(criteria, draft.MinBedrooms.HasValue,
            "bedrooms",
            candidate.Bedrooms >= draft.MinBedrooms,
            "Đủ số phòng ngủ");
        Add(criteria, draft.FurnishingLevel.HasValue,
            "furnishing",
            candidate.FurnishingLevel >= draft.FurnishingLevel,
            "Đáp ứng mức nội thất");
        Add(criteria, draft.MinArea.HasValue || draft.MaxArea.HasValue,
            "areaRange",
            IsWithin(candidate.Area, draft.MinArea, draft.MaxArea),
            "Diện tích phù hợp");
        Add(criteria, draft.AllowsPets == true,
            "pets",
            candidate.AllowsPets,
            "Cho phép nuôi thú cưng");
        Add(criteria, draft.ParkingType.HasValue,
            "parking",
            candidate.ParkingType >= draft.ParkingType,
            "Đáp ứng chỗ đậu xe");
        Add(criteria, draft.AvailableBy.HasValue,
            "moveInDate",
            candidate.AvailableFrom <= draft.AvailableBy,
            "Có thể vào ở đúng thời gian");
        Add(criteria, draft.MinFloor.HasValue || draft.MaxFloor.HasValue,
            "floorRange",
            candidate.FloorNumber.HasValue &&
            IsWithin(candidate.FloorNumber.Value, draft.MinFloor, draft.MaxFloor),
            "Nằm trong khoảng tầng mong muốn");
        Add(criteria, draft.HouseDirection.HasValue,
            "direction",
            candidate.HouseDirection == draft.HouseDirection,
            "Đúng hướng nhà mong muốn");
        Add(criteria, draft.MinLeaseMonths.HasValue || draft.MaxLeaseMonths.HasValue,
            "leaseRange",
            LeaseRangesOverlap(candidate, draft),
            "Thời hạn thuê phù hợp");

        return criteria;
    }

    private static bool DistanceMatches(
        RentalMatchCandidate candidate,
        RentalPreferenceDraft draft)
    {
        if (!draft.MaxDistanceKm.HasValue ||
            !GeoDistance.IsValidCoordinate(draft.PreferredLatitude, draft.PreferredLongitude) ||
            !GeoDistance.IsValidCoordinate(candidate.Latitude, candidate.Longitude))
        {
            return false;
        }

        return GeoDistance.CalculateKm(
            draft.PreferredLatitude!.Value,
            draft.PreferredLongitude!.Value,
            candidate.Latitude!.Value,
            candidate.Longitude!.Value) <= draft.MaxDistanceKm.Value;
    }

    private static bool LeaseRangesOverlap(
        RentalMatchCandidate candidate,
        RentalPreferenceDraft draft)
    {
        var desiredMin = draft.MinLeaseMonths ?? 1;
        var desiredMax = draft.MaxLeaseMonths ?? int.MaxValue;
        return candidate.MinLeaseMonths <= desiredMax &&
               candidate.MaxLeaseMonths >= desiredMin;
    }

    private static bool IsWithin<T>(T value, T? min, T? max)
        where T : struct, IComparable<T>
    {
        return (!min.HasValue || value.CompareTo(min.Value) >= 0) &&
               (!max.HasValue || value.CompareTo(max.Value) <= 0);
    }

    private static void Add(
        ICollection<Criterion> criteria,
        bool isActive,
        string key,
        bool isMatch,
        string reason)
    {
        if (isActive)
            criteria.Add(new Criterion(key, isMatch, reason));
    }

    private static RentalMatchResult Rejected()
    {
        return new RentalMatchResult(false, 0, Array.Empty<string>());
    }

    private sealed record Criterion(string Key, bool IsMatch, string Reason);
}
