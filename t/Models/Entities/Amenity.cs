using t.Models.Entities.Common;

namespace t.Models.Entities;

public class Amenity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    public ICollection<ApartmentAmenity> ApartmentAmenities { get; set; } = new List<ApartmentAmenity>();
}
