using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum InspectionType { MoveIn = 0, MoveOut = 1 }

public enum OverallCondition
{
    Excellent = 0,
    Good = 1,
    Fair = 2,
    Poor = 3
}

public class LeaseInspection : BaseEntity
{
    public int LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public InspectionType Type { get; set; } = InspectionType.MoveIn;
    public DateTime InspectedAt { get; set; } = DateTime.UtcNow;

    public string? InspectorId { get; set; }
    public AppUser? Inspector { get; set; }

    public OverallCondition OverallCondition { get; set; } = OverallCondition.Good;
    public string? Summary { get; set; }
    public string? DamageNotes { get; set; }
    public string? PhotoUrls { get; set; }

    public decimal DepositDeduction { get; set; }
    public bool TenantSigned { get; set; }
}
