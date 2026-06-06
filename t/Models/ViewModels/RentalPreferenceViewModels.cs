using t.Infrastructure.Geo;
using t.Models.Entities;
using Microsoft.AspNetCore.Routing;

namespace t.Models.ViewModels;

public sealed class RentalSearchRequest
{
    public string? Keyword { get; set; }
    public string? Region { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinArea { get; set; }
    public double? MaxArea { get; set; }
    public List<int> CategoryIds { get; set; } = new();
    public List<int> AmenityIds { get; set; } = new();
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Category { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public int? RegionId { get; set; }
    public int? MinBedrooms { get; set; }
    public FurnishingLevel? FurnishingLevel { get; set; }
    public bool? AllowsPets { get; set; }
    public ParkingType? ParkingType { get; set; }
    public DateOnly? AvailableBy { get; set; }
    public string? PreferredAddress { get; set; }
    public double? PreferredLatitude { get; set; }
    public double? PreferredLongitude { get; set; }
    public double? MaxDistanceKm { get; set; }
    public int? MinFloor { get; set; }
    public int? MaxFloor { get; set; }
    public HouseDirection? HouseDirection { get; set; }
    public int? MinLeaseMonths { get; set; }
    public int? MaxLeaseMonths { get; set; }
    public List<string> RequiredCriteria { get; set; } = new();
    public List<int> RequiredAmenityIds { get; set; } = new();
    public bool PendingPreferenceSave { get; set; }

    public RouteValueDictionary ToRouteValues(int? page = null, bool clearPendingSave = false)
    {
        return new RouteValueDictionary
        {
            [nameof(Keyword)] = Keyword,
            [nameof(Region)] = Region,
            [nameof(MinPrice)] = MinPrice,
            [nameof(MaxPrice)] = MaxPrice,
            [nameof(MinArea)] = MinArea,
            [nameof(MaxArea)] = MaxArea,
            [nameof(CategoryIds)] = CategoryIds,
            [nameof(AmenityIds)] = AmenityIds,
            [nameof(Sort)] = Sort,
            [nameof(Page)] = page ?? Page,
            [nameof(Category)] = Category,
            [nameof(Latitude)] = Latitude,
            [nameof(Longitude)] = Longitude,
            [nameof(RegionId)] = RegionId,
            [nameof(MinBedrooms)] = MinBedrooms,
            [nameof(FurnishingLevel)] = FurnishingLevel,
            [nameof(AllowsPets)] = AllowsPets,
            [nameof(ParkingType)] = ParkingType,
            [nameof(AvailableBy)] = AvailableBy,
            [nameof(PreferredAddress)] = PreferredAddress,
            [nameof(PreferredLatitude)] = PreferredLatitude,
            [nameof(PreferredLongitude)] = PreferredLongitude,
            [nameof(MaxDistanceKm)] = MaxDistanceKm,
            [nameof(MinFloor)] = MinFloor,
            [nameof(MaxFloor)] = MaxFloor,
            [nameof(HouseDirection)] = HouseDirection,
            [nameof(MinLeaseMonths)] = MinLeaseMonths,
            [nameof(MaxLeaseMonths)] = MaxLeaseMonths,
            [nameof(RequiredCriteria)] = RequiredCriteria,
            [nameof(RequiredAmenityIds)] = RequiredAmenityIds,
            [nameof(PendingPreferenceSave)] = clearPendingSave ? null : PendingPreferenceSave ? true : null
        };
    }

    public static RentalSearchRequest FromDraft(RentalPreferenceDraft draft)
    {
        return new RentalSearchRequest
        {
            Sort = "match_desc",
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
}

public sealed class RentalPreferenceDraft
{
    public int? RegionId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinArea { get; set; }
    public double? MaxArea { get; set; }
    public int? MinBedrooms { get; set; }
    public IReadOnlySet<int> CategoryIds { get; set; } = new HashSet<int>();
    public IReadOnlySet<int> AmenityIds { get; set; } = new HashSet<int>();
    public IReadOnlySet<int> RequiredAmenityIds { get; set; } = new HashSet<int>();
    public FurnishingLevel? FurnishingLevel { get; set; }
    public bool? AllowsPets { get; set; }
    public ParkingType? ParkingType { get; set; }
    public DateOnly? AvailableBy { get; set; }
    public string? PreferredAddress { get; set; }
    public double? PreferredLatitude { get; set; }
    public double? PreferredLongitude { get; set; }
    public double? MaxDistanceKm { get; set; }
    public int? MinFloor { get; set; }
    public int? MaxFloor { get; set; }
    public HouseDirection? HouseDirection { get; set; }
    public int? MinLeaseMonths { get; set; }
    public int? MaxLeaseMonths { get; set; }
    public IReadOnlySet<string> RequiredCriteria { get; set; } = new HashSet<string>();
}

public static class RentalPreferenceCriteria
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "region", "priceRange", "areaRange", "bedrooms", "category",
        "maxDistance", "furnishing", "pets", "parking", "moveInDate",
        "floorRange", "direction", "leaseRange"
    };

    public static string GetLabel(string criterion) => criterion switch
    {
        "region" => "Khu vực",
        "priceRange" => "Khoảng giá",
        "areaRange" => "Khoảng diện tích",
        "bedrooms" => "Số phòng ngủ",
        "category" => "Loại hình",
        "maxDistance" => "Khoảng cách tối đa",
        "furnishing" => "Nội thất",
        "pets" => "Cho phép thú cưng",
        "parking" => "Chỗ đậu xe",
        "moveInDate" => "Ngày vào ở",
        "floorRange" => "Khoảng tầng",
        "direction" => "Hướng nhà",
        "leaseRange" => "Thời hạn thuê",
        _ => criterion
    };
}

public sealed record RentalPreferenceNormalizationResult(
    RentalPreferenceDraft Draft,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}

public static class RentalPreferenceNormalizer
{
    public static RentalPreferenceNormalizationResult Normalize(RentalSearchRequest request, bool strict)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        NormalizeEmptyPriceRange(request);
        ValidateRange(request.MinPrice, request.MaxPrice, "Khoảng giá", errors);
        ValidateRange(request.MinArea, request.MaxArea, "Khoảng diện tích", errors);
        ValidateRange(request.MinFloor, request.MaxFloor, "Khoảng tầng", errors);
        ValidateRange(request.MinLeaseMonths, request.MaxLeaseMonths, "Thời hạn thuê", errors, 1);

        if (request.MinBedrooms < 0)
            errors.Add("Số phòng ngủ tối thiểu không hợp lệ.");
        if (request.MaxDistanceKm <= 0)
            errors.Add("Bán kính tối đa phải lớn hơn 0.");
        if (request.FurnishingLevel.HasValue && !Enum.IsDefined(request.FurnishingLevel.Value))
            errors.Add("Mức nội thất không hợp lệ.");
        if (request.ParkingType.HasValue && !Enum.IsDefined(request.ParkingType.Value))
            errors.Add("Loại chỗ đậu xe không hợp lệ.");
        if (request.HouseDirection.HasValue && !Enum.IsDefined(request.HouseDirection.Value))
            errors.Add("Hướng nhà không hợp lệ.");

        var coordinates = GeoDistance.ValidatePair(request.PreferredLatitude, request.PreferredLongitude);
        if (!coordinates.IsValid)
        {
            AddIssue(strict, errors, warnings, "Không thể xác định tọa độ cho địa điểm đã nhập; đã bỏ điều kiện bán kính.");
        }

        var amenities = request.AmenityIds.Where(id => id > 0).ToHashSet();
        var requiredAmenities = request.RequiredAmenityIds.Where(id => id > 0).ToHashSet();
        if (!requiredAmenities.IsSubsetOf(amenities))
            errors.Add("Tiện ích bắt buộc phải nằm trong danh sách tiện ích mong muốn.");

        var requiredCriteria = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in request.RequiredCriteria.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            if (!RentalPreferenceCriteria.Allowed.Contains(key))
            {
                AddIssue(strict, errors, warnings, $"Tiêu chí bắt buộc '{key}' không hợp lệ.");
                continue;
            }

            if (!HasValue(key, request, coordinates.IsActive))
            {
                warnings.Add($"Đã bỏ tiêu chí bắt buộc '{key}' vì chưa có giá trị.");
                continue;
            }

            requiredCriteria.Add(key);
        }

        return new RentalPreferenceNormalizationResult(
            new RentalPreferenceDraft
            {
                RegionId = request.RegionId,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                MinArea = request.MinArea,
                MaxArea = request.MaxArea,
                MinBedrooms = request.MinBedrooms,
                CategoryIds = request.CategoryIds.Where(id => id > 0).ToHashSet(),
                AmenityIds = amenities,
                RequiredAmenityIds = requiredAmenities,
                FurnishingLevel = request.FurnishingLevel,
                AllowsPets = request.AllowsPets == true ? true : null,
                ParkingType = request.ParkingType,
                AvailableBy = request.AvailableBy,
                PreferredAddress = request.PreferredAddress?.Trim(),
                PreferredLatitude = coordinates.IsActive ? request.PreferredLatitude : null,
                PreferredLongitude = coordinates.IsActive ? request.PreferredLongitude : null,
                MaxDistanceKm = coordinates.IsActive && request.MaxDistanceKm > 0 ? request.MaxDistanceKm : null,
                MinFloor = request.MinFloor,
                MaxFloor = request.MaxFloor,
                HouseDirection = request.HouseDirection,
                MinLeaseMonths = request.MinLeaseMonths,
                MaxLeaseMonths = request.MaxLeaseMonths,
                RequiredCriteria = requiredCriteria
            },
            errors,
            warnings);
    }

    private static void NormalizeEmptyPriceRange(RentalSearchRequest request)
    {
        if (request.MinPrice <= 0)
            request.MinPrice = null;
        if (request.MaxPrice <= 0)
            request.MaxPrice = null;
    }

    private static bool HasValue(string key, RentalSearchRequest request, bool hasPreferredCoordinates)
    {
        return key switch
        {
            "region" => request.RegionId.HasValue,
            "priceRange" => request.MinPrice.HasValue || request.MaxPrice.HasValue,
            "areaRange" => request.MinArea.HasValue || request.MaxArea.HasValue,
            "bedrooms" => request.MinBedrooms.HasValue,
            "category" => request.CategoryIds.Count > 0,
            "maxDistance" => hasPreferredCoordinates && request.MaxDistanceKm > 0,
            "furnishing" => request.FurnishingLevel.HasValue,
            "pets" => request.AllowsPets == true,
            "parking" => request.ParkingType.HasValue,
            "moveInDate" => request.AvailableBy.HasValue,
            "floorRange" => request.MinFloor.HasValue || request.MaxFloor.HasValue,
            "direction" => request.HouseDirection.HasValue,
            "leaseRange" => request.MinLeaseMonths.HasValue || request.MaxLeaseMonths.HasValue,
            _ => false
        };
    }

    private static void ValidateRange<T>(
        T? min,
        T? max,
        string label,
        ICollection<string> errors,
        T? lowest = default)
        where T : struct, IComparable<T>
    {
        if (lowest.HasValue && min.HasValue && min.Value.CompareTo(lowest.Value) < 0)
            errors.Add($"{label}: giá trị tối thiểu không hợp lệ.");
        if (lowest.HasValue && max.HasValue && max.Value.CompareTo(lowest.Value) < 0)
            errors.Add($"{label}: giá trị tối đa không hợp lệ.");
        if (min.HasValue && max.HasValue && min.Value.CompareTo(max.Value) > 0)
            errors.Add($"{label}: giá trị tối thiểu không được lớn hơn tối đa.");
    }

    private static void AddIssue(
        bool strict,
        ICollection<string> errors,
        ICollection<string> warnings,
        string message)
    {
        if (strict)
            errors.Add(message);
        else
            warnings.Add(message);
    }
}
