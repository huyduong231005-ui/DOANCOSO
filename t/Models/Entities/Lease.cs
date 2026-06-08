using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum LeaseStatus
{
    Pending = 0,
    Active = 1,
    Expired = 2,
    Terminated = 3,
    Renewing = 4
}

public class Lease : BaseEntity
{
    public string LeaseNumber { get; set; } = string.Empty;

    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public string PrimaryTenantId { get; set; } = string.Empty;
    public AppUser PrimaryTenant { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal MonthlyRent { get; set; }
    public decimal Deposit { get; set; }
    public decimal DepositHeld { get; set; }
    public decimal DepositRefunded { get; set; }

    public int BillingDay { get; set; } = 1;
    public int LateFeePercent { get; set; } = 5;
    public int LateFeeAfterDays { get; set; } = 7;

    public LeaseStatus Status { get; set; } = LeaseStatus.Pending;

    public DateTime? ActivatedAt { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public string? TerminationReason { get; set; }

    public string? ContractUrl { get; set; }
    public string? Notes { get; set; }

    public ICollection<LeaseTenant> AdditionalTenants { get; set; } = new List<LeaseTenant>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<UtilityReading> UtilityReadings { get; set; } = new List<UtilityReading>();
    public ICollection<RecurringCharge> RecurringCharges { get; set; } = new List<RecurringCharge>();
    public ICollection<LeaseInspection> Inspections { get; set; } = new List<LeaseInspection>();
    public ICollection<DepositTransaction> DepositTransactions { get; set; } = new List<DepositTransaction>();
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}
