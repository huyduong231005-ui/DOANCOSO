using t.Models.Entities.Common;

namespace t.Models.Entities;

public class Floor : BaseEntity
{
    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;

    public int Number { get; set; }
    public string Label => $"Tầng {Number}";

    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
}
