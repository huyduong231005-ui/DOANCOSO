using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum BuildingStatus { Active = 0, UnderMaintenance = 1, Closed = 2 }

public class Building : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int RegionId { get; set; }
    public Region Region { get; set; } = null!;

    public string Address { get; set; } = string.Empty;
    public int FloorCount { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Description { get; set; }

    public string? ManagerId { get; set; }
    public AppUser? Manager { get; set; }

    public BuildingStatus Status { get; set; } = BuildingStatus.Active;

    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
}
