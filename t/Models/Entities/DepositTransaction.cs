using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum DepositTransactionType
{
    Hold = 0,
    Refund = 1,
    Forfeit = 2,
    Deduction = 3,
    Adjustment = 4
}

public class DepositTransaction : BaseEntity
{
    public int LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public DepositTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;

    public int? RelatedInspectionId { get; set; }
    public LeaseInspection? RelatedInspection { get; set; }

    public int? RelatedRequestId { get; set; }
    public MaintenanceRequest? RelatedRequest { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string? RecordedBy { get; set; }
}
