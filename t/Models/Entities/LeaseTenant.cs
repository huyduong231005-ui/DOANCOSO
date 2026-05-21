using t.Models.Entities.Common;

namespace t.Models.Entities;

public class LeaseTenant : IAuditable
{
    public int LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public string TenantId { get; set; } = string.Empty;
    public AppUser Tenant { get; set; } = null!;

    public string? Relationship { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
