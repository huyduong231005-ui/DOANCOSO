using t.Models.Entities.Common;

namespace t.Models.Entities;

public class Favorite : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;
}
