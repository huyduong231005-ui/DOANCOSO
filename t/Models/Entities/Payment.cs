using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    CreditCard = 2,
    Wallet = 3,
    QrCode = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Refunded = 3,
    PartiallyRefunded = 4,
    Cancelled = 5
}

public class Payment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? TransactionRef { get; set; }
    public string? Provider { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? Note { get; set; }
}
