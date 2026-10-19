using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class ReportsController : AdminBaseController
{
    public ReportsController(AppDbContext db) : base(db) { }

    public IActionResult Index() => RedirectToAction(nameof(Occupancy));

    public async Task<IActionResult> Occupancy()
    {
        SetActiveNav("reports");
        SetBreadcrumb(("Báo cáo", null), ("Tình trạng căn hộ", null));

        var rows = await Db.Buildings
            .Select(b => new OccupancyRow
            {
                BuildingId = b.Id,
                BuildingName = b.Name,
                Total = b.Apartments.Count(),
                Occupied = b.Apartments.Count(a => a.Occupancy == ApartmentOccupancy.Occupied),
                Vacant = b.Apartments.Count(a => a.Occupancy == ApartmentOccupancy.Vacant),
                NotHandedOver = b.Apartments.Count(a => a.Occupancy == ApartmentOccupancy.NotHandedOver),
                UnderMaintenance = b.Apartments.Count(a => a.Occupancy == ApartmentOccupancy.UnderMaintenance),
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

        // Aggregate KPIs
        var thisMonthBilled = await Db.Invoices
            .Where(i => i.IssueDate >= startOfMonth && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.Total) ?? 0m;
        var thisMonthPaid = await Db.Payments
            .Where(p => p.PaidAt != null && p.PaidAt >= startOfMonth && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)(p.Amount - p.RefundedAmount)) ?? 0m;
        var prevMonthPaid = await Db.Payments
            .Where(p => p.PaidAt != null && p.PaidAt >= startOfPrevMonth && p.PaidAt < startOfMonth && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)(p.Amount - p.RefundedAmount)) ?? 0m;
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
            var paid = await Db.Payments
                .Where(p => p.PaidAt != null && p.PaidAt >= mStart && p.PaidAt < mEnd && p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => (decimal?)(p.Amount - p.RefundedAmount)) ?? 0m;
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
            .GroupBy(i => new { i.ApartmentId, i.Apartment.UnitCode, BuildingName = i.Apartment.Building.Name })
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

    public async Task<IActionResult> Arrears()
    {
        SetActiveNav("reports");
        SetBreadcrumb(("Báo cáo", null), ("Công nợ", null));

        var now = DateTime.UtcNow.Date;
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
                BuildingName = i.Apartment.Building.Name,
                RepresentativeName = i.Apartment.PrimaryResident?.FullName,
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
