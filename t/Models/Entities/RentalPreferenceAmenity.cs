using t.Models.Entities.Common;

namespace t.Models.Entities;

public sealed class RentalPreferenceAmenity : IAuditable
{
    public int ProfileId { get; set; }
    public RentalPreferenceProfile Profile { get; set; } = null!;
    public int AmenityId { get; set; }
    public Amenity Amenity { get; set; } = null!;
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
