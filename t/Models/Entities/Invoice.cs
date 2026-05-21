using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5,
    Refunded = 6
}

public enum InvoiceKind
{
    MonthlyRent = 0,
    Deposit = 1,
    LateFee = 2,
    OneOff = 3
}

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public int LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public InvoiceKind Kind { get; set; } = InvoiceKind.MonthlyRent;
    public int BillingMonth { get; set; }
    public bool IsRecurring { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }

    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal LateFee { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string Currency { get; set; } = "VND";
    public string? Note { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
