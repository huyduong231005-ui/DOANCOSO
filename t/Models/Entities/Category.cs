using t.Models.Entities.Common;

namespace t.Models.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }

    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
}
