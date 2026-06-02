using Microsoft.AspNetCore.Identity;
using t.Models.Entities.Common;

namespace t.Models.Entities;

public class AppUser : IdentityUser, IAuditable, ISoftDeletable
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public bool IsHost { get; set; }
    public string? HostTitle { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
    public ICollection<LeaseTenant> CoTenancies { get; set; } = new List<LeaseTenant>();
    public ICollection<Building> ManagedBuildings { get; set; } = new List<Building>();
    public RentalPreferenceProfile? RentalPreferenceProfile { get; set; }
}
