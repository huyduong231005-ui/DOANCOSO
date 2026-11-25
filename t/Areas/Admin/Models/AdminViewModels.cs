using System.ComponentModel.DataAnnotations;
using t.Models.Entities;

namespace t.Areas.Admin.Models;

public class PageInfo
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public Func<int, string> Url { get; set; } = p => "?page=" + p;
}

public class DashboardViewModel
{
    public int BuildingsTotal { get; set; }
    public int UnitsTotal { get; set; }
    public int UnitsOccupied { get; set; }
    public int UnitsAvailable { get; set; }
    public double OccupancyRate { get; set; }
    public int ResidentsTotal { get; set; }
    public int MaintenanceOpen { get; set; }
    public int MaintenanceOverdue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal ArrearsTotal { get; set; }
    public int InvoicesUnpaid { get; set; }

    // ── Phân tích tài chính nâng cao ──
    public decimal RevenueLastMonth { get; set; }          // Doanh thu tháng trước (để tính MoM)
    public decimal RevenueAvgPerMonth { get; set; }        // Trung bình doanh thu/tháng (12 tháng)
    public double MoMGrowthPct { get; set; }               // % tăng/giảm so với tháng trước
    public double CollectionRate { get; set; }             // Tỉ lệ thu thành công = AmountPaid / Total

    public decimal BreakdownManagement { get; set; }       // Phí quản lý (cộng dồn)
    public decimal BreakdownUtilities { get; set; }        // Điện + Nước
    public decimal BreakdownServices { get; set; }         // Gửi xe + phí cố định + khác

    public List<RecentMaintenanceRow> RecentMaintenance { get; set; } = new();
    public List<TopBuildingRow> TopBuildings { get; set; } = new();
    public List<RevenueByDayRow> RevenueByDay { get; set; } = new();
    public List<RevenueByMonthRow> RevenueByMonth { get; set; } = new();
}

public class RevenueByMonthRow
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Total { get; set; }
    public string Label => $"{Month:00}/{Year}";
}


public class RecentMaintenanceRow
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
}

public class TopBuildingRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Units { get; set; }
    public int Occupied { get; set; }
    public decimal MonthlyRevenue { get; set; }
}

public class RevenueByDayRow
{
    public DateTime Day { get; set; }
    public decimal Total { get; set; }
}

// ───────────────────── Buildings ─────────────────────

public class BuildingListVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? ManagerName { get; set; }
    public BuildingStatus Status { get; set; }
    public int FloorCount { get; set; }
    public int UnitsTotal { get; set; }
    public int UnitsOccupied { get; set; }
    public int UnitsVacant { get; set; }
    public decimal Outstanding { get; set; }
    public int OpenMaintenance { get; set; }
    public double OccupancyRate => UnitsTotal == 0 ? 0 : Math.Round(UnitsOccupied * 100.0 / UnitsTotal, 1);
}

public class BuildingEditVm
{
    public int Id { get; set; }
    [Required, StringLength(40)] public string Code { get; set; } = string.Empty;
    [Required(ErrorMessage = "Chọn khu chứa toà.")] public int? ProjectId { get; set; }
    [Range(1, 120)] public int FloorCount { get; set; } = 1;
    public string? ManagerId { get; set; }
    public BuildingStatus Status { get; set; } = BuildingStatus.Active;

    [Range(1, 28)] public int DefaultBillingDay { get; set; } = 1;
    [Range(0, 60)] public int DefaultLateFeeAfterDays { get; set; } = 7;
    [Range(0, 100)] public int DefaultLateFeePercent { get; set; } = 5;
    [Range(0, 1_000_000)] public decimal ManagementFeePerM2 { get; set; }
    [Range(0, 60)] public int UnitsPerFloor { get; set; }
    [Range(0, 1000)] public decimal DefaultUnitArea { get; set; } = 70;

    public List<Project> Projects { get; set; } = new();
    public List<HostOption> Managers { get; set; } = new();
}

public class BuildingPlanVm
{
    public Building Building { get; set; } = null!;
    public List<FloorRowVm> Floors { get; set; } = new();
    public int ResidentCount { get; set; }
    public decimal MonthlyFeeEstimate { get; set; }

    public IEnumerable<UnitCellVm> AllUnits => Floors.SelectMany(f => f.Units);
    public int Count(ApartmentOccupancy o) => AllUnits.Count(u => u.Occupancy == o);
}

public class FloorRowVm
{
    public Floor Floor { get; set; } = null!;
    public List<UnitCellVm> Units { get; set; } = new();
}

public class UnitCellVm
{
    public int Id { get; set; }
    public int FloorId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public int Bedrooms { get; set; }
    public ApartmentCondition Condition { get; set; }
    public ApartmentOccupancy Occupancy => Apartment.OccupancyOf(Condition, ResidentCount > 0);
    public bool IsRented { get; set; }
    public int ResidentCount { get; set; }
    public string? RepresentativeName { get; set; }
    public decimal Balance { get; set; }
    public bool HasOverdue { get; set; }
    public int OpenMaintenance { get; set; }

    // Số căn trên tầng (phần sau dấu chấm của mã A-12.05 → 05) để ô gọn.
    public string ShortCode => UnitCode.Contains('.') ? UnitCode[(UnitCode.LastIndexOf('.') + 1)..] : UnitCode;
}

// ───────────────────── Apartments (Units) ─────────────────────

public class ApartmentListVm
{
    public int Id { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public decimal Area { get; set; }
    public int Bedrooms { get; set; }
    public ApartmentCondition Condition { get; set; }
    public bool HasResident { get; set; }
    public ApartmentOccupancy Occupancy => Apartment.OccupancyOf(Condition, HasResident);
    public string? RepresentativeName { get; set; }
    public int ResidentCount { get; set; }
    public decimal Balance { get; set; }
}

public class ApartmentEditVm
{
    public int Id { get; set; }
    [Required, StringLength(40)] public string UnitCode { get; set; } = string.Empty;
    [Required] public int BuildingId { get; set; }
    [Required] public int FloorId { get; set; }
    [Range(1, 1000)] public decimal Area { get; set; }
    [Range(0, 20)] public int Bedrooms { get; set; }
    public ApartmentCondition Condition { get; set; } = ApartmentCondition.HandedOver;

    public List<Building> Buildings { get; set; } = new();
    public List<Floor> Floors { get; set; } = new();
}

public class ApartmentDetailVm
{
    public Apartment Apartment { get; set; } = null!;
    public List<Invoice> Invoices { get; set; } = new();
    public List<UtilityReading> Readings { get; set; } = new();
    public List<MaintenanceRequest> Maintenance { get; set; } = new();
    public decimal Balance { get; set; }
    public decimal OverdueBalance { get; set; }
    public ResidentEditVm NewResident { get; set; } = new();
    public RecurringChargeEditVm NewCharge { get; set; } = new();

    public decimal ManagementFee => Math.Round((decimal)Apartment.Area * Apartment.Floor.Building.ManagementFeePerM2, 0);
    public decimal MonthlyFixed => ManagementFee + Apartment.RecurringCharges.Where(c => c.IsActive).Sum(c => c.Amount);
}

public class ResidentEditVm
{
    public int Id { get; set; }
    public int ApartmentId { get; set; }
    [Required, StringLength(150)] public string FullName { get; set; } = string.Empty;
    [StringLength(30)] public string? Phone { get; set; }
    [EmailAddress, StringLength(200)] public string? Email { get; set; }
    [StringLength(30)] public string? IdNumber { get; set; }
    public ResidentRole Role { get; set; } = ResidentRole.Owner;
    public bool IsPrimary { get; set; }
    public DateTime MoveInDate { get; set; } = DateTime.Today;
}

public class RecurringChargeEditVm
{
    public int ApartmentId { get; set; }
    [Required, StringLength(200)] public string Description { get; set; } = string.Empty;
    [Range(0, 100_000_000)] public decimal Amount { get; set; }
}

public class ResidentListVm
{
    public int Id { get; set; }
    public int ApartmentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public ResidentRole Role { get; set; }
    public bool IsPrimary { get; set; }
    public bool HasAccount { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public DateTime MoveInDate { get; set; }
}

public class HostOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

// ───────────────────── Projects ─────────────────────

public class ProjectListVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int BuildingCount { get; set; }
    public int ApartmentCount { get; set; }
    public int OccupiedCount { get; set; }
    public int ResidentCount { get; set; }
    public decimal Outstanding { get; set; }
    public double OccupancyRate => ApartmentCount == 0 ? 0 : Math.Round(OccupiedCount * 100.0 / ApartmentCount, 1);
}

public class ProjectEditVm
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    [Required] public string Address { get; set; } = string.Empty;
}

// ───────────────────── Invoices / Payments ─────────────────────

public class InvoiceListVm
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int ApartmentId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public InvoiceKind Kind { get; set; }
    public int BillingMonth { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public InvoiceStatus Status { get; set; }
}

public class InvoiceDetailVm
{
    public Invoice Invoice { get; set; } = null!;
}

public class RecordPaymentVm
{
    public int InvoiceId { get; set; }
    [Range(1, double.MaxValue)] public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
    public string? TransactionRef { get; set; }
    public string? Note { get; set; }
}

public class ApartmentOption
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class OneOffInvoiceLine
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
}

public class OneOffInvoiceVm
{
    public int ApartmentId { get; set; }
    public string? Title { get; set; }
    public DateTime DueDate { get; set; }
    public string? Note { get; set; }
    public List<OneOffInvoiceLine> Lines { get; set; } = new();
    public List<ApartmentOption> Apartments { get; set; } = new();
}

public class EditInvoiceItemsVm
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? Note { get; set; }
    public List<OneOffInvoiceLine> Lines { get; set; } = new();
}

public class PaymentListVm
{
    public int Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? TransactionRef { get; set; }
}

// Aggregated row for the Payments index — one row per outstanding invoice.
public class PaymentInvoiceListVm
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int ApartmentId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public InvoiceKind Kind { get; set; }
    public int BillingMonth { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal ManagementAmount { get; set; }
    public decimal UtilityAmount { get; set; }
    public decimal OtherAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public InvoiceStatus Status { get; set; }
    public bool HasPendingPayment { get; set; }
}

// ───────────────────── Utilities ─────────────────────

public class UtilityTypeEditVm
{
    public int Id { get; set; }
    [Required, StringLength(40)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    [StringLength(20)] public string Unit { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal DefaultRate { get; set; }
}

public class UtilityReadingListVm
{
    public int Id { get; set; }
    public int ApartmentId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string UtilityName { get; set; } = string.Empty;
    public int BillingMonth { get; set; }
    public decimal Previous { get; set; }
    public decimal Current { get; set; }
    public decimal Consumption { get; set; }
    public decimal Amount { get; set; }
    public bool Billed { get; set; }
}

public class UtilityReadingEditVm
{
    public int Id { get; set; }
    public int ApartmentId { get; set; }
    public int UtilityTypeId { get; set; }
    [Range(202000, 209912)] public int BillingMonth { get; set; }
    [Range(0, double.MaxValue)] public decimal PreviousReading { get; set; }
    [Range(0, double.MaxValue)] public decimal CurrentReading { get; set; }

    public List<ApartmentOption> Apartments { get; set; } = new();
    public List<UtilityType> UtilityTypes { get; set; } = new();
}

public class BulkReadingVm
{
    public int BuildingId { get; set; }
    public int UtilityTypeId { get; set; }
    public int BillingMonth { get; set; }
    public decimal Rate { get; set; }
    public List<BulkReadingRow> Rows { get; set; } = new();

    public List<Building> Buildings { get; set; } = new();
    public List<UtilityType> UtilityTypes { get; set; } = new();
}

public class BulkReadingRow
{
    public int ApartmentId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public decimal PreviousReading { get; set; }
    public decimal? CurrentReading { get; set; }
    public bool Locked { get; set; }
}

// ───────────────────── Reports ─────────────────────

public class OccupancyRow
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Occupied { get; set; }
    public int Vacant { get; set; }
    public int NotHandedOver { get; set; }
    public int UnderMaintenance { get; set; }
    public int Rented { get; set; }
    public decimal Outstanding { get; set; }
    public double Rate => Total == 0 ? 0 : Math.Round(Occupied * 100.0 / Total, 1);
}

public class MonthlyFinanceRow
{
    public DateTime Month { get; set; }
    public decimal Billed { get; set; }
    public decimal Paid { get; set; }
}

public class KindBreakdownRow
{
    public InvoiceKind Kind { get; set; }
    public int Count { get; set; }
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
}

public class TopDebtorRow
{
    public int ApartmentId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalBalance { get; set; }
}

public class FinanceDashboardVm
{
    public decimal ThisMonthBilled { get; set; }
    public decimal ThisMonthPaid { get; set; }
    /// <summary>Số đã thu của riêng các hoá đơn phát hành tháng này.</summary>
    public decimal ThisMonthBilledCollected { get; set; }
    public decimal PrevMonthPaid { get; set; }
    public decimal Outstanding { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal PendingPaymentsAmount { get; set; }
    public int PendingPaymentsCount { get; set; }
    public List<MonthlyFinanceRow> Months { get; set; } = new();
    public List<KindBreakdownRow> Kinds { get; set; } = new();
    public List<TopDebtorRow> TopDebtors { get; set; } = new();
}

public class ArrearsRow
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int ApartmentId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string? BuildingName { get; set; }
    public string? RepresentativeName { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysLate { get; set; }
    public decimal Balance { get; set; }
    public string Bucket { get; set; } = string.Empty;
}

// ───────────────────── Maintenance ─────────────────────

public class MaintenanceListVm
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? ApartmentId { get; set; }
    public string? UnitCode { get; set; }
    public string? Location { get; set; }
    public string? BuildingName { get; set; }
    public string? ReporterName { get; set; }
    public string? AssignedToName { get; set; }
    public MaintenanceCategory Category { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public string Place => UnitCode ?? "Khu vực chung: " + Location;
    public bool IsOverdue => DueAt != null && (ResolvedAt ?? DateTime.UtcNow) > DueAt
        && Status is not (MaintenanceStatus.Rejected or MaintenanceStatus.Cancelled);
}

public class MaintenanceEditVm
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int? ApartmentId { get; set; }
    public int? CommonBuildingId { get; set; }
    [StringLength(200)] public string? Location { get; set; }
    public string? ReporterId { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required] public string Description { get; set; } = string.Empty;
    public MaintenanceCategory Category { get; set; } = MaintenanceCategory.Other;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public string? PhotoUrls { get; set; }

    public List<ApartmentOption> Apartments { get; set; } = new();
    public List<ApartmentOption> Buildings { get; set; } = new();
    public List<HostOption> Reporters { get; set; } = new();
}

public class AssignMaintenanceVm
{
    public int Id { get; set; }
    [Required] public string AssignedToId { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class ResolveMaintenanceVm
{
    public int Id { get; set; }
    [Required] public string ResolutionNote { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal ActualCost { get; set; }
    public bool ChargeToTenant { get; set; }
    public string? PhotoUrls { get; set; }
}

// ───────────────────── Users / Roles / Audit ─────────────────────

public class UserListVm
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool LockedOut { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? ApartmentCodes { get; set; }
}

public class UserEditVm
{
    public string Id { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool LockedOut { get; set; }
    public List<string> SelectedRoles { get; set; } = new();
    public List<string> AllRoles { get; set; } = new();
}

public class RoleMatrixVm
{
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, List<PermissionRow>> ByModule { get; set; } = new();
}

public class PermissionRow
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, bool> Assignments { get; set; } = new();
}

public class AuditListVm
{
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityKey { get; set; }
    public AuditAction Action { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}
