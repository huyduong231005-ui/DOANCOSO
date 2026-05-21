using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum MaintenanceCategory
{
    Plumbing = 0,
    Electrical = 1,
    Appliance = 2,
    Structural = 3,
    Cleaning = 4,
    Pest = 5,
    Network = 6,
    Other = 99
}

public enum MaintenancePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

public enum MaintenanceStatus
{
    Open = 0,
    Acknowledged = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4,
    Rejected = 5
}

public class MaintenanceRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;

    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public int? LeaseId { get; set; }
    public Lease? Lease { get; set; }

    public string? ReporterId { get; set; }
    public AppUser? Reporter { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public MaintenanceCategory Category { get; set; } = MaintenanceCategory.Other;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

    public string? AssignedToId { get; set; }
    public AppUser? AssignedTo { get; set; }

    public string? ResolutionNote { get; set; }
    public string? PhotoUrls { get; set; }

    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public bool ChargeToTenant { get; set; }

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
