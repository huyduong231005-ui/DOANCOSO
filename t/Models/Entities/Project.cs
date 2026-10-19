using t.Models.Entities.Common;

namespace t.Models.Entities;

/// <summary>Khu căn hộ (vd: một khu đô thị gồm nhiều toà A, B, C).</summary>
public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;

    public Region Region { get; set; } = null!;
    public ICollection<Building> Buildings { get; set; } = new List<Building>();
}
