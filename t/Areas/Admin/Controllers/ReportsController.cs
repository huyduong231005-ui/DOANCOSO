using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Maintenance;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class ReportsController : AdminBaseController
{
    public ReportsController(AppDbContext db) : base(db) { }

    public IActionResult Index() => RedirectToAction(nameof(Occupancy));

    /// <summary>
    /// KPI quy trình sự cố (FR-09) cho các yêu cầu báo trong kỳ [from, to] theo giờ Việt Nam.
    /// ponytail: đọc mọi yêu cầu của kỳ vào bộ nhớ rồi tính (vài nghìn dòng/tháng là nhẹ);
    /// khi dữ liệu lên hàng trăm nghìn dòng mỗi kỳ thì chuyển các phép đếm sang GROUP BY trong SQL.
    /// </summary>
    public async Task<IActionResult> Maintenance(DateTime? from, DateTime? to, int? buildingId)
    {
        SetActiveNav("reports");
        SetBreadcrumb(("Báo cáo", null), ("Sự cố", null));

        var today = VnTime.Today;
        var fromDate = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var toDate = (to ?? today).Date;
        if (toDate < fromDate) (fromDate, toDate) = (toDate, fromDate);
        var fromUtc = VnTime.ToUtc(fromDate);
        var toUtc = VnTime.ToUtc(toDate.AddDays(1));

        var query = Db.MaintenanceRequests.AsNoTracking().Where(r => r.ReportedAt >= fromUtc && r.ReportedAt < toUtc);
        if (buildingId.HasValue)
            query = query.Where(r => (r.Apartment != null && r.Apartment.BuildingId == buildingId) || r.CommonBuildingId == buildingId);

        var rows = await query.Select(r => new MaintenanceKpiRow
        {
            Id = r.Id, Status = r.Status, Priority = r.Priority, Category = r.Category,
            BuildingName = r.Apartment != null ? "Toà " + r.Apartment.Building.Code : r.CommonBuilding != null ? "Toà " + r.CommonBuilding.Code : null,
            TechnicianName = r.AssignedTo != null ? r.AssignedTo.FullName : null,
            // Mốc tiếp nhận, giải quyết lấy từ lịch sử trạng thái (không lưu cột riêng).
            ReportedAt = r.ReportedAt, AcknowledgedAt = r.History.Where(h => h.ToStatus == MaintenanceStatus.Acknowledged).Min(h => (DateTime?)h.ChangedAt),
            ResolvedAt = (r.Status == MaintenanceStatus.Resolved || r.Status == MaintenanceStatus.Closed ? r.History.Where(h => h.ToStatus == MaintenanceStatus.Resolved).Max(h => (DateTime?)h.ChangedAt) : null), DueAt = r.DueAt,
            ResponseSlaMinutes = Db.SlaPolicies.Where(s => s.Priority == r.Priority).Select(s => s.ResponseMinutes).FirstOrDefault(),
            Rating = r.Rating,
            AutoAssigned = r.History.Any(h => h.ChangedById == null && h.Note != null && h.Note.Contains(MaintenanceDispatcher.AutoAssignNote)),
            // Giao lại = dòng do người thực hiện mà trạng thái giữ nguyên so với dòng liền trước.
            Reassigned = r.History.Any(h => h.ChangedById != null && r.History
                .Where(p => p.ChangedAt < h.ChangedAt || (p.ChangedAt == h.ChangedAt && p.Id < h.Id))
                .OrderByDescending(p => p.ChangedAt).ThenByDescending(p => p.Id)
                .Select(p => (MaintenanceStatus?)p.ToStatus).FirstOrDefault() == h.ToStatus),
        }).ToListAsync();

        ViewBag.From = fromDate;
        ViewBag.To = toDate;
        ViewBag.BuildingId = buildingId;
        ViewBag.Buildings = await Db.Buildings.AsNoTracking().OrderBy(b => b.Code).ToListAsync();
        return View(MaintenanceKpi.Compute(rows, DateTime.UtcNow));
    }

    public async Task<IActionResult> Occupancy()
    {
        SetActiveNav("reports");
        SetBreadcrumb(("Báo cáo", null), ("Tình trạng căn hộ", null));

        var rows = await Db.Buildings
            .Select(b => new OccupancyRow
            {
                BuildingId = b.Id,
                BuildingName = "Toà " + b.Code,
                Total = b.Apartments.Count(),
                Occupied = b.Apartments.AsQueryable().Count(Apartment.IsOccupied),
                Vacant = b.Apartments.AsQueryable().Count(Apartment.IsVacant),
                NotHandedOver = b.Apartments.Count(a => a.Condition == ApartmentCondition.NotHandedOver),
                UnderMaintenance = b.Apartments.Count(a => a.Condition == ApartmentCondition.UnderMaintenance),
                Rented = b.Apartments.Count(a => a.Residents.Any(r => r.MoveOutDate == null && r.Role == ResidentRole.Tenant)),
                Outstanding = b.Apartments.SelectMany(a => a.Invoices)
                    .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                    .Sum(i => (decimal?)i.Balance) ?? 0m
            })
            .OrderBy(x => x.BuildingName)
            .ToListAsync();

        return View(rows);
    }

    public async Task<IActionResult> Finance()
    {
        SetActiveNav("reports");
        SetBreadcrumb(("Báo cáo", null), ("Tài chính", null));

        var today = VnTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var startOfPrevMonth = startOfMonth.AddMonths(-1);

        // IssueDate lưu theo giờ Việt Nam, PaidAt theo UTC: mốc so sánh của tiền thu phải đổi sang UTC.
        // Thực thu = số đã trả trừ phần đã hoàn, tính cả thanh toán đã hoàn một phần.
        var monthStartUtc = VnTime.ToUtc(startOfMonth);
        var thisMonthBilled = await Db.Invoices
            .Where(i => i.IssueDate >= startOfMonth && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.Total) ?? 0m;
        // Tỉ lệ thu chỉ xét tiền đã thu của chính các hoá đơn phát hành tháng này (không lẫn tiền nợ cũ).
        var thisMonthBilledCollected = await Db.Invoices
            .Where(i => i.IssueDate >= startOfMonth && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.AmountPaid) ?? 0m;
        var thisMonthPaid = await CollectedAsync(monthStartUtc, null);
        var prevMonthPaid = await CollectedAsync(VnTime.ToUtc(startOfPrevMonth), monthStartUtc);
        var outstanding = await Db.Invoices
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
            .SumAsync(i => (decimal?)i.Balance) ?? 0m;
        var overdueAmount = await Db.Invoices
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                        && i.DueDate < today)
            .SumAsync(i => (decimal?)i.Balance) ?? 0m;
        var pendingPaymentsAmount = await Db.Payments
            .Where(p => p.Status == PaymentStatus.Pending)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        var pendingPaymentsCount = await Db.Payments.CountAsync(p => p.Status == PaymentStatus.Pending);

        // Last 6 months: paid + billed
        var months = Enumerable.Range(0, 6).Select(i => startOfMonth.AddMonths(-i)).Reverse().ToList();
        var monthlyRows = new List<MonthlyFinanceRow>();
        foreach (var m in months)
        {
            var mStart = m;
            var mEnd = m.AddMonths(1);
            var billed = await Db.Invoices
                .Where(i => i.IssueDate >= mStart && i.IssueDate < mEnd && i.Status != InvoiceStatus.Cancelled)
                .SumAsync(i => (decimal?)i.Total) ?? 0m;
            var paid = await CollectedAsync(VnTime.ToUtc(mStart), VnTime.ToUtc(mEnd));
            monthlyRows.Add(new MonthlyFinanceRow { Month = m, Billed = billed, Paid = paid });
        }

        // Breakdown by kind this month
        var kindRows = await Db.Invoices
            .Where(i => i.IssueDate >= startOfMonth && i.Status != InvoiceStatus.Cancelled)
            .GroupBy(i => i.Kind)
            .Select(g => new KindBreakdownRow
            {
                Kind = g.Key,
                Count = g.Count(),
                Total = g.Sum(x => x.Total),
                Paid = g.Sum(x => x.AmountPaid),
                Balance = g.Sum(x => x.Balance)
            })
            .ToListAsync();

        // Top debtors
        var topDebtors = await Db.Invoices
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
            .GroupBy(i => new { i.ApartmentId, i.Apartment.UnitCode, BuildingName = "Toà " + i.Apartment.Building.Code })
            .Select(g => new TopDebtorRow
            {
                ApartmentId = g.Key.ApartmentId,
                UnitCode = g.Key.UnitCode,
                BuildingName = g.Key.BuildingName,
                InvoiceCount = g.Count(),
                TotalBalance = g.Sum(x => x.Balance)
            })
            .OrderByDescending(x => x.TotalBalance)
            .Take(10)
            .ToListAsync();

        var vm = new FinanceDashboardVm
        {
            ThisMonthBilled = thisMonthBilled,
            ThisMonthPaid = thisMonthPaid,
            ThisMonthBilledCollected = thisMonthBilledCollected,
            PrevMonthPaid = prevMonthPaid,
            Outstanding = outstanding,
            OverdueAmount = overdueAmount,
            PendingPaymentsAmount = pendingPaymentsAmount,
            PendingPaymentsCount = pendingPaymentsCount,
            Months = monthlyRows,
            Kinds = kindRows,
            TopDebtors = topDebtors
        };
        return View(vm);
    }

    /// <summary>Thực thu trong khoảng [fromUtc, toUtc): đã trả trừ đã hoàn, gồm cả thanh toán đã hoàn một phần.</summary>
    private async Task<decimal> CollectedAsync(DateTime fromUtc, DateTime? toUtc) =>
        await Db.Payments
            .Where(p => p.PaidAt != null && p.PaidAt >= fromUtc && (toUtc == null || p.PaidAt < toUtc)
                        && (p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.PartiallyRefunded))
            .SumAsync(p => (decimal?)(p.Amount - p.RefundedAmount)) ?? 0m;

    public async Task<IActionResult> Arrears()
    {
        SetActiveNav("reports");
        SetBreadcrumb(("Báo cáo", null), ("Công nợ", null));

        var now = VnTime.Today; // hạn thanh toán là ngày theo giờ Việt Nam
        var open = await Db.Invoices
            .Include(i => i.Apartment).ThenInclude(a => a.Building)
            .Include(i => i.Apartment).ThenInclude(a => a.Residents)
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                        && i.Balance > 0)
            .ToListAsync();

        int Bucket(int days) => days <= 0 ? 0 : days <= 30 ? 1 : days <= 60 ? 2 : days <= 90 ? 3 : 4;
        var buckets = new[] { "Chưa đến hạn", "1-30 ngày", "31-60 ngày", "61-90 ngày", ">90 ngày" };
        var totals = new decimal[5];

        var rows = open.Select(i =>
        {
            var days = (int)(now - i.DueDate).TotalDays;
            var b = Bucket(days);
            totals[b] += i.Balance;
            return new ArrearsRow
            {
                InvoiceId = i.Id, InvoiceNumber = i.InvoiceNumber,
                ApartmentId = i.ApartmentId,
                UnitCode = i.Apartment.UnitCode,
                BuildingName = "Toà " + i.Apartment.Building.Code,
                RepresentativeName = i.Apartment.PrimaryResident?.User.FullName,
                DueDate = i.DueDate, DaysLate = Math.Max(0, days),
                Balance = i.Balance,
                Bucket = buckets[b]
            };
        })
        .OrderByDescending(x => x.DaysLate)
        .ToList();

        ViewBag.BucketLabels = buckets;
        ViewBag.BucketTotals = totals;
        ViewBag.GrandTotal = totals.Sum();
        return View(rows);
    }
}
