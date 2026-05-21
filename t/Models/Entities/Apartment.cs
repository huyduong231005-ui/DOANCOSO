using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum ListingStatus { Draft, Active, Expired, Hidden }

public enum ApartmentOccupancy
{
    Available = 0,
    Reserved = 1,
    Occupied = 2,
    UnderMaintenance = 3
}

public class Apartment : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? UnitCode { get; set; }
    public string? Description { get; set; }
    public string? DescriptionExtra { get; set; }
    public decimal Price { get; set; }
    public decimal? DefaultDeposit { get; set; }
    public string? FeeNote { get; set; }
    public double Area { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Draft;
    public ApartmentOccupancy Occupancy { get; set; } = ApartmentOccupancy.Available;
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }

    // Moderation
    public string? ModerationNote { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    public string HostId { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public int CategoryId { get; set; }
    public int? ProjectId { get; set; }
    public int? BuildingId { get; set; }
    public int? FloorId { get; set; }

    public AppUser Host { get; set; } = null!;
    public Region Region { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Project? Project { get; set; }
    public Building? Building { get; set; }
    public Floor? Floor { get; set; }
    public ICollection<ApartmentImage> Images { get; set; } = new List<ApartmentImage>();
    public ICollection<ApartmentAmenity> ApartmentAmenities { get; set; } = new List<ApartmentAmenity>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
}
