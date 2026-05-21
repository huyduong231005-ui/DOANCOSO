using t.Models.Entities.Common;

namespace t.Models.Entities;

public class Floor : BaseEntity
{
    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;

    public int Number { get; set; }
    public string Label { get; set; } = string.Empty;

    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
}
