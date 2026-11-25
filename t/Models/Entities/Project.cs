using t.Models.Entities.Common;

namespace t.Models.Entities;

/// <summary>Khu căn hộ (vd: một khu đô thị gồm nhiều toà A, B, C).</summary>
public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public ICollection<Building> Buildings { get; set; } = new List<Building>();
}
