using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly UserManager<AppUser> _userManager;

    public DashboardController(AppDbContext db, UserManager<AppUser> userManager) : base(db)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        SetActiveNav("dashboard");
        SetBreadcrumb(("Dashboard", null));

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var soonThreshold = now.AddDays(30);

        var unitsTotal = await Db.Apartments.CountAsync();
        var unitsOccupied = await Db.Apartments.CountAsync(a => a.Occupancy == ApartmentOccupancy.Occupied);
        var unitsAvailable = await Db.Apartments.CountAsync(a => a.Occupancy == ApartmentOccupancy.Available);

        var vm = new DashboardViewModel
        {
            BuildingsTotal = await Db.Buildings.CountAsync(),
            UnitsTotal = unitsTotal,
            UnitsOccupied = unitsOccupied,
            UnitsAvailable = unitsAvailable,
            OccupancyRate = unitsTotal == 0 ? 0 : Math.Round(unitsOccupied * 100.0 / unitsTotal, 1),
            LeasesActive = await Db.Leases.CountAsync(l => l.Status == LeaseStatus.Active),
            LeasesExpiringSoon = await Db.Leases.CountAsync(l =>
                l.Status == LeaseStatus.Active && l.EndDate <= soonThreshold && l.EndDate >= now),
            RevenueThisMonth = await Db.Payments
                .Where(p => p.Status == PaymentStatus.Succeeded && p.PaidAt >= startOfMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m,
            ArrearsTotal = await Db.Invoices
                .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                .SumAsync(i => (decimal?)i.Balance) ?? 0m,
            InvoicesUnpaid = await Db.Invoices.CountAsync(i =>
                i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue),
            ReviewsPending = await Db.Reviews.CountAsync(r => r.Status == ReviewStatus.Pending),
            UsersTotal = await _userManager.Users.CountAsync()
        };

        vm.RecentLeases = await Db.Leases
            .OrderByDescending(l => l.CreatedAt)
            .Take(8)
            .Select(l => new RecentLeaseRow
            {
                Id = l.Id, LeaseNumber = l.LeaseNumber,
                TenantName = l.PrimaryTenant.FullName,
                UnitTitle = l.Apartment.Title,
                BuildingName = l.Apartment.Building != null ? l.Apartment.Building.Name : string.Empty,
                StartDate = l.StartDate, EndDate = l.EndDate,
                MonthlyRent = l.MonthlyRent, Status = l.Status
            })
            .ToListAsync();

        vm.ExpiringLeases = await Db.Leases
            .Where(l => l.Status == LeaseStatus.Active && l.EndDate >= now && l.EndDate <= soonThreshold)
            .OrderBy(l => l.EndDate)
            .Take(6)
            .Select(l => new ExpiringLeaseRow
            {
                Id = l.Id, LeaseNumber = l.LeaseNumber,
                TenantName = l.PrimaryTenant.FullName,
                UnitTitle = l.Apartment.Title,
                EndDate = l.EndDate,
                DaysLeft = (int)(l.EndDate - now).TotalDays
            })
            .ToListAsync();

        vm.TopBuildings = await Db.Buildings
            .Select(b => new TopBuildingRow
            {
                Id = b.Id, Name = b.Name,
                Units = b.Apartments.Count(),
                Occupied = b.Apartments.Count(a => a.Occupancy == ApartmentOccupancy.Occupied),
                MonthlyRevenue = b.Apartments
                    .SelectMany(a => a.Leases)
                    .Where(l => l.Status == LeaseStatus.Active)
                    .Sum(l => (decimal?)l.MonthlyRent) ?? 0m
            })
            .OrderByDescending(x => x.MonthlyRevenue)
            .Take(5)
            .ToListAsync();

        var startOfWindow = now.Date.AddDays(-13);
        var revenueRaw = await Db.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded && p.PaidAt != null && p.PaidAt >= startOfWindow)
            .GroupBy(p => p.PaidAt!.Value.Date)
            .Select(g => new RevenueByDayRow { Day = g.Key, Total = g.Sum(p => p.Amount) })
            .ToListAsync();

        vm.RevenueByDay = Enumerable.Range(0, 14)
            .Select(i => startOfWindow.AddDays(i))
            .Select(day => revenueRaw.FirstOrDefault(r => r.Day == day) ?? new RevenueByDayRow { Day = day, Total = 0 })
            .ToList();

        // ─────────────────────────────────────────────────────────────
        // PHÂN TÍCH TÀI CHÍNH NÂNG CAO
        // ─────────────────────────────────────────────────────────────
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var startOf30Days = now.AddDays(-30);
        var startOf12Months = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        vm.RevenueAllTime = await Db.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        vm.RevenueLast30Days = await Db.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded && p.PaidAt != null && p.PaidAt >= startOf30Days)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        vm.RevenueLastMonth = await Db.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded && p.PaidAt != null
                     && p.PaidAt >= startOfLastMonth && p.PaidAt < startOfMonth)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        vm.MoMGrowthPct = vm.RevenueLastMonth == 0 ? 0
            : Math.Round((double)((vm.RevenueThisMonth - vm.RevenueLastMonth) * 100m / vm.RevenueLastMonth), 1);

        // Doanh thu trung bình 12 tháng (chỉ tính các tháng có dữ liệu)
        var revenueByMonthRaw = await Db.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded && p.PaidAt != null && p.PaidAt >= startOf12Months)
            .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt.Value.Month })
            .Select(g => new RevenueByMonthRow
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(p => p.Amount)
            })
            .ToListAsync();

        vm.RevenueByMonth = Enumerable.Range(0, 12)
            .Select(i => startOf12Months.AddMonths(i))
            .Select(d => revenueByMonthRaw.FirstOrDefault(r => r.Year == d.Year && r.Month == d.Month)
                          ?? new RevenueByMonthRow { Year = d.Year, Month = d.Month, Total = 0 })
            .ToList();

        var nonZeroMonths = vm.RevenueByMonth.Count(r => r.Total > 0);
        vm.RevenueAvgPerMonth = nonZeroMonths == 0 ? 0
            : Math.Round(vm.RevenueByMonth.Sum(r => r.Total) / nonZeroMonths, 0);

        // Dự kiến tháng tới: tổng MonthlyRent của các hợp đồng đang Active
        vm.RevenueForecastNext = await Db.Leases
            .Where(l => l.Status == LeaseStatus.Active)
            .SumAsync(l => (decimal?)l.MonthlyRent) ?? 0m;

        // Tỉ lệ thu thành công: AmountPaid / Total trên toàn bộ hoá đơn đã phát hành (loại trừ Draft/Cancelled)
        var invoiceTotals = await Db.Invoices
            .Where(i => i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(i => i.Total), Paid = g.Sum(i => i.AmountPaid) })
            .FirstOrDefaultAsync();
        vm.CollectionRate = invoiceTotals == null || invoiceTotals.Total == 0 ? 0
            : Math.Round((double)(invoiceTotals.Paid * 100m / invoiceTotals.Total), 1);

        // Breakdown doanh thu theo loại (dựa trên InvoiceItem.SortOrder của các hoá đơn đã có thu tiền):
        //   SortOrder 0       → Tiền thuê
        //   SortOrder 1..2    → Tiện ích (điện, nước)
        //   SortOrder 3+      → Dịch vụ (internet, phí khác)
        // Tính tỉ lệ trên mỗi hoá đơn rồi nhân với AmountPaid để phân bổ đúng.
        var paidInvoiceItems = await Db.Invoices
            .Where(i => i.AmountPaid > 0)
            .Select(i => new
            {
                i.AmountPaid,
                i.Total,
                Items = i.Items.Select(it => new { it.SortOrder, it.LineTotal })
            })
            .ToListAsync();
        decimal sumRent = 0, sumUtil = 0, sumSvc = 0;
        foreach (var i in paidInvoiceItems)
        {
            if (i.Total <= 0) continue;
            var ratio = i.AmountPaid / i.Total;
            foreach (var it in i.Items)
            {
                var allocated = it.LineTotal * ratio;
                if (it.SortOrder == 0) sumRent += allocated;
                else if (it.SortOrder <= 2) sumUtil += allocated;
                else sumSvc += allocated;
            }
        }
        vm.BreakdownRent = Math.Round(sumRent, 0);
        vm.BreakdownUtilities = Math.Round(sumUtil, 0);
        vm.BreakdownServices = Math.Round(sumSvc, 0);

        // Top 5 căn hộ doanh thu cao nhất (qua toàn bộ lịch sử)
        vm.TopApartments = await Db.Invoices
            .Where(i => i.AmountPaid > 0)
            .GroupBy(i => i.Lease.ApartmentId)
            .Select(g => new
            {
                ApartmentId = g.Key,
                Collected = g.Sum(i => i.AmountPaid),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Collected)
            .Take(5)
            .Join(Db.Apartments.Include(a => a.Building),
                  agg => agg.ApartmentId,
                  apt => apt.Id,
                  (agg, apt) => new TopApartmentRow
                  {
                      Id = apt.Id,
                      Title = apt.Title,
                      UnitCode = apt.UnitCode,
                      BuildingName = apt.Building != null ? apt.Building.Name : null,
                      TotalCollected = agg.Collected,
                      InvoiceCount = agg.Count
                  })
            .ToListAsync();

        return View(vm);
    }
}
