using t.Models.Entities.Common;

namespace t.Models.Entities;

public class ApartmentAmenity : IAuditable
{
    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public int AmenityId { get; set; }
    public Amenity Amenity { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
