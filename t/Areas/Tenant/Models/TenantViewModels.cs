using System.ComponentModel.DataAnnotations;
using t.Models.Entities;

namespace t.Areas.Tenant.Models;

public class TenantDashboardVm
{
    public Lease? ActiveLease { get; set; }
    public int InvoicesUnpaidCount { get; set; }
    public decimal TotalDue { get; set; }
    public DateTime? NextDueDate { get; set; }
    public int OpenMaintenanceCount { get; set; }
    public List<TenantInvoiceRow> RecentInvoices { get; set; } = new();
    public List<TenantMaintenanceRow> RecentMaintenance { get; set; } = new();
}

public class TenantLeaseHistoryRow
{
    public int Id { get; set; }
    public string LeaseNumber { get; set; } = string.Empty;
    public string ApartmentTitle { get; set; } = string.Empty;
    public string? BuildingName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public LeaseStatus Status { get; set; }
    public bool IsCoTenant { get; set; }
}

public class TenantInvoiceRow
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceKind Kind { get; set; }
    public int BillingMonth { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal Balance { get; set; }
    public InvoiceStatus Status { get; set; }
}

public class TenantMaintenanceRow
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public MaintenanceCategory Category { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
}

public class CreateMaintenanceVm
{
    [Required, StringLength(250)] public string Title { get; set; } = string.Empty;
    [Required] public string Description { get; set; } = string.Empty;
    public MaintenanceCategory Category { get; set; } = MaintenanceCategory.Other;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public string? PhotoUrls { get; set; }
}

public class SubmitPaymentVm
{
    public int InvoiceId { get; set; }
    [Range(1, double.MaxValue)] public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
    public string? TransactionRef { get; set; }
    public string? Note { get; set; }
}

public class TenantProfileVm
{
    [Required] public string FullName { get; set; } = string.Empty;
    [EmailAddress] public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}
