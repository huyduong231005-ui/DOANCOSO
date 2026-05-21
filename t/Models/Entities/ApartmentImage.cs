using t.Models.Entities.Common;

namespace t.Models.Entities;

public class ApartmentImage : BaseEntity
{
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }

    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;
}
