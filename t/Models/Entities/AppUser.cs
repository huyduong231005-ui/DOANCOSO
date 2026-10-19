using Microsoft.AspNetCore.Identity;
using t.Models.Entities.Common;

namespace t.Models.Entities;

public class AppUser : IdentityUser, IAuditable, ISoftDeletable
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public ICollection<Building> ManagedBuildings { get; set; } = new List<Building>();
    public ICollection<ApartmentResident> Residencies { get; set; } = new List<ApartmentResident>();
}
