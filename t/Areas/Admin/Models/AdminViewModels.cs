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
    public int LeasesActive { get; set; }
    public int LeasesExpiringSoon { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal ArrearsTotal { get; set; }
    public int InvoicesUnpaid { get; set; }
    public int ReviewsPending { get; set; }
    public int UsersTotal { get; set; }

    // ── Phân tích tài chính nâng cao ──
    public decimal RevenueAllTime { get; set; }            // Tổng doanh thu cộng dồn từ trước đến giờ
    public decimal RevenueLast30Days { get; set; }         // Doanh thu 30 ngày gần nhất
    public decimal RevenueLastMonth { get; set; }          // Doanh thu tháng trước (để tính MoM)
    public decimal RevenueAvgPerMonth { get; set; }        // Trung bình doanh thu/tháng (12 tháng)
    public decimal RevenueForecastNext { get; set; }       // Dự kiến tháng tới (sum MonthlyRent active leases)
    public double MoMGrowthPct { get; set; }               // % tăng/giảm so với tháng trước
    public double CollectionRate { get; set; }             // Tỉ lệ thu thành công = AmountPaid / Total

    public decimal BreakdownRent { get; set; }             // Tiền thuê (cộng dồn)
    public decimal BreakdownUtilities { get; set; }        // Điện + Nước
    public decimal BreakdownServices { get; set; }         // Internet + Dịch vụ + khác

    public List<RecentLeaseRow> RecentLeases { get; set; } = new();
    public List<TopBuildingRow> TopBuildings { get; set; } = new();
    public List<RevenueByDayRow> RevenueByDay { get; set; } = new();
    public List<RevenueByMonthRow> RevenueByMonth { get; set; } = new();
    public List<TopApartmentRow> TopApartments { get; set; } = new();
    public List<ExpiringLeaseRow> ExpiringLeases { get; set; } = new();
}

public class RevenueByMonthRow
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Total { get; set; }
    public string Label => $"{Month:00}/{Year}";
}

public class TopApartmentRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? UnitCode { get; set; }
    public string? BuildingName { get; set; }
    public decimal TotalCollected { get; set; }
    public int InvoiceCount { get; set; }
}

public class RecentLeaseRow
{
    public int Id { get; set; }
    public string LeaseNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public LeaseStatus Status { get; set; }
}

public class ExpiringLeaseRow
{
    public int Id { get; set; }
    public string LeaseNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysLeft { get; set; }
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
    public string RegionName { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? ManagerName { get; set; }
    public BuildingStatus Status { get; set; }
    public int UnitsTotal { get; set; }
    public int UnitsOccupied { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BuildingEditVm
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(220)] public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public int RegionId { get; set; }
    public string Address { get; set; } = string.Empty;
    public int FloorCount { get; set; } = 1;
    public string? ThumbnailUrl { get; set; }
    public string? Description { get; set; }
    public string? ManagerId { get; set; }
    public BuildingStatus Status { get; set; } = BuildingStatus.Active;

    [Range(1, 28)] public int DefaultBillingDay { get; set; } = 1;
    [Range(0, 60)] public int DefaultLateFeeAfterDays { get; set; } = 7;
    [Range(0, 100)] public int DefaultLateFeePercent { get; set; } = 5;

    public List<Region> Regions { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
    public List<HostOption> Managers { get; set; } = new();
}

// ───────────────────── Apartments (Units) ─────────────────────

public class ApartmentListVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? UnitCode { get; set; }
    public string CoverUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? BuildingName { get; set; }
    public string? FloorLabel { get; set; }
    public string HostName { get; set; } = string.Empty;
    public ListingStatus Status { get; set; }
    public ApartmentOccupancy Occupancy { get; set; }
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApartmentEditVm
{
    public int Id { get; set; }
    [Required, StringLength(250)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(280)] public string Slug { get; set; } = string.Empty;
    [StringLength(40)] public string? UnitCode { get; set; }
    public string? Description { get; set; }
    public string? DescriptionExtra { get; set; }
    [Range(0, double.MaxValue)] public decimal Price { get; set; }
    public decimal? DefaultDeposit { get; set; }
    public string? FeeNote { get; set; }
    [Range(0, 1000)] public double Area { get; set; }
    [Range(0, 20)] public int Bedrooms { get; set; }
    [Range(0, 20)] public int Bathrooms { get; set; }
    [Required] public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public FurnishingLevel FurnishingLevel { get; set; }
    public bool AllowsPets { get; set; }
    public ParkingType ParkingType { get; set; }
    public DateOnly AvailableFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [Range(1, 120)] public int MinLeaseMonths { get; set; } = 1;
    [Range(1, 120)] public int MaxLeaseMonths { get; set; } = 12;
    public HouseDirection? HouseDirection { get; set; }
    [Range(0, 500)] public int? FloorNumber { get; set; }
    public ListingStatus Status { get; set; }
    public ApartmentOccupancy Occupancy { get; set; }
    public bool IsFeatured { get; set; }
    [Required] public string HostId { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public int CategoryId { get; set; }
    public int? ProjectId { get; set; }
    public int? BuildingId { get; set; }
    public int? FloorId { get; set; }
    public List<int> AmenityIds { get; set; } = new();

    public List<Region> Regions { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
    public List<Building> Buildings { get; set; } = new();
    public List<Floor> Floors { get; set; } = new();
    public List<Amenity> Amenities { get; set; } = new();
    public List<HostOption> Hosts { get; set; } = new();
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
    public string Slug { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public decimal PriceFrom { get; set; }
    public int ApartmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProjectEditVm
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(220)] public string Slug { get; set; } = string.Empty;
    public int RegionId { get; set; }
    [Required] public string Address { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal PriceFrom { get; set; }
    public ProjectStatus Status { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public List<Region> Regions { get; set; } = new();
}

// ───────────────────── Leases ─────────────────────

public class LeaseListVm
{
    public int Id { get; set; }
    public string LeaseNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantEmail { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public int ApartmentId { get; set; }
    public string? BuildingName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal Deposit { get; set; }
    public LeaseStatus Status { get; set; }
}

public class LeaseEditVm
{
    public int Id { get; set; }
    public string LeaseNumber { get; set; } = string.Empty;
    public int ApartmentId { get; set; }
    [Required] public string PrimaryTenantId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddYears(1);
    [Range(0, double.MaxValue)] public decimal MonthlyRent { get; set; }
    [Range(0, double.MaxValue)] public decimal Deposit { get; set; }
    [Range(1, 28)] public int BillingDay { get; set; } = 1;
    [Range(0, 100)] public int LateFeePercent { get; set; } = 5;
    [Range(0, 60)] public int LateFeeAfterDays { get; set; } = 7;
    public string? ContractUrl { get; set; }
    public string? Notes { get; set; }
    public LeaseStatus Status { get; set; } = LeaseStatus.Pending;
    public List<string> CoTenantIds { get; set; } = new();

    public List<Apartment> Apartments { get; set; } = new();
    public List<HostOption> Tenants { get; set; } = new();
}

public class TerminateLeaseVm
{
    public int Id { get; set; }
    public DateTime TerminatedAt { get; set; } = DateTime.UtcNow.Date;
    public string? Reason { get; set; }
    [Range(0, double.MaxValue)] public decimal RefundAmount { get; set; }
}

// ───────────────────── Invoices / Payments ─────────────────────

public class InvoiceListVm
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string LeaseNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
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

public class LeaseOption
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
    public int LeaseId { get; set; }
    public string? Title { get; set; }
    public DateTime DueDate { get; set; }
    public string? Note { get; set; }
    public List<OneOffInvoiceLine> Lines { get; set; } = new();
    public List<LeaseOption> Leases { get; set; } = new();
}

public class EditInvoiceItemsVm
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public string? Note { get; set; }
    public List<OneOffInvoiceLine> Lines { get; set; } = new();
}

public class PaymentListVm
{
    public int Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
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
    public string LeaseNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public InvoiceKind Kind { get; set; }
    public int BillingMonth { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal RentAmount { get; set; }
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
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [StringLength(20)] public string Unit { get; set; } = string.Empty;
    public UtilityBillingMode BillingMode { get; set; } = UtilityBillingMode.Metered;
    [Range(0, double.MaxValue)] public decimal DefaultRate { get; set; }
    public string? Icon { get; set; }
}

public class UtilityReadingListVm
{
    public int Id { get; set; }
    public int LeaseId { get; set; }
    public string LeaseNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public string UtilityName { get; set; } = string.Empty;
    public string UtilityIcon { get; set; } = string.Empty;
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
    public int LeaseId { get; set; }
    public int UtilityTypeId { get; set; }
    [Range(202000, 209912)] public int BillingMonth { get; set; }
    [Range(0, double.MaxValue)] public decimal PreviousReading { get; set; }
    [Range(0, double.MaxValue)] public decimal CurrentReading { get; set; }
    [Range(0, double.MaxValue)] public decimal Rate { get; set; }
    public string? Note { get; set; }

    public List<Lease> Leases { get; set; } = new();
    public List<UtilityType> UtilityTypes { get; set; } = new();
}

// ───────────────────── Reports ─────────────────────

public class OccupancyRow
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Occupied { get; set; }
    public int Available { get; set; }
    public int Reserved { get; set; }
    public int UnderMaintenance { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public double Rate => Total == 0 ? 0 : Math.Round(Occupied * 100.0 / Total, 1);
}

public class ForecastRow
{
    public DateTime Month { get; set; }
    public int ActiveLeases { get; set; }
    public decimal ProjectedRevenue { get; set; }
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
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalBalance { get; set; }
}

public class FinanceDashboardVm
{
    public decimal ThisMonthBilled { get; set; }
    public decimal ThisMonthPaid { get; set; }
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
    public string LeaseNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string? BuildingName { get; set; }
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
    public int ApartmentId { get; set; }
    public string UnitTitle { get; set; } = string.Empty;
    public string? UnitCode { get; set; }
    public string? BuildingName { get; set; }
    public string? ReporterName { get; set; }
    public string? AssignedToName { get; set; }
    public MaintenanceCategory Category { get; set; }
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class MaintenanceEditVm
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    [Required] public int ApartmentId { get; set; }
    public int? LeaseId { get; set; }
    public string? ReporterId { get; set; }
    [Required, StringLength(250)] public string Title { get; set; } = string.Empty;
    [Required] public string Description { get; set; } = string.Empty;
    public MaintenanceCategory Category { get; set; } = MaintenanceCategory.Other;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public string? PhotoUrls { get; set; }
    [Range(0, double.MaxValue)] public decimal EstimatedCost { get; set; }

    public List<Apartment> Apartments { get; set; } = new();
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
    public bool DeductFromDeposit { get; set; }
}

// ───────────────────── Inspection / Deposit ─────────────────────

public class InspectionEditVm
{
    public int Id { get; set; }
    public int LeaseId { get; set; }
    public InspectionType Type { get; set; } = InspectionType.MoveIn;
    public DateTime InspectedAt { get; set; } = DateTime.UtcNow.Date;
    public OverallCondition OverallCondition { get; set; } = OverallCondition.Good;
    public string? Summary { get; set; }
    public string? DamageNotes { get; set; }
    public string? PhotoUrls { get; set; }
    [Range(0, double.MaxValue)] public decimal DepositDeduction { get; set; }
    public bool TenantSigned { get; set; }
}

public class DepositLedgerRow
{
    public DateTime At { get; set; }
    public DepositTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RecordedByName { get; set; }
}

// ───────────────────── Reviews / Users / Roles / Audit ─────────────────────

public class ReviewListVm
{
    public int Id { get; set; }
    public string ApartmentTitle { get; set; } = string.Empty;
    public int ApartmentId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public ReviewStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserListVm
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsHost { get; set; }
    public bool LockedOut { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public int LeasesCount { get; set; }
}

public class UserEditVm
{
    public string Id { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsHost { get; set; }
    public string? HostTitle { get; set; }
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
    public string? ChangedColumns { get; set; }
}
