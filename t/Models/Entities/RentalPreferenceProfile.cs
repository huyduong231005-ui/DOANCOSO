using t.Models.Entities.Common;

namespace t.Models.Entities;

public sealed class RentalPreferenceProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public int? RegionId { get; set; }
    public Region? Region { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinArea { get; set; }
    public double? MaxArea { get; set; }
    public int? MinBedrooms { get; set; }
    public string? PreferredAddress { get; set; }
    public double? PreferredLatitude { get; set; }
    public double? PreferredLongitude { get; set; }
    public double? MaxDistanceKm { get; set; }
    public FurnishingLevel? FurnishingLevel { get; set; }
    public bool? AllowsPets { get; set; }
    public ParkingType? ParkingType { get; set; }
    public DateOnly? MoveInDate { get; set; }
    public int? MinFloor { get; set; }
    public int? MaxFloor { get; set; }
    public HouseDirection? HouseDirection { get; set; }
    public int? MinLeaseMonths { get; set; }
    public int? MaxLeaseMonths { get; set; }

    public bool RequireRegion { get; set; }
    public bool RequirePriceRange { get; set; }
    public bool RequireAreaRange { get; set; }
    public bool RequireBedrooms { get; set; }
    public bool RequireCategoryMatch { get; set; }
    public bool RequireMaxDistance { get; set; }
    public bool RequireFurnishing { get; set; }
    public bool RequirePets { get; set; }
    public bool RequireParking { get; set; }
    public bool RequireMoveInDate { get; set; }
    public bool RequireFloorRange { get; set; }
    public bool RequireDirection { get; set; }
    public bool RequireLeaseRange { get; set; }

    public ICollection<RentalPreferenceCategory> Categories { get; set; } = new List<RentalPreferenceCategory>();
    public ICollection<RentalPreferenceAmenity> Amenities { get; set; } = new List<RentalPreferenceAmenity>();
}
