using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    public DashboardController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index()
    {
        SetActiveNav("dashboard");
        SetBreadcrumb(("Bảng điều khiển", null));

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var unitsTotal = await Db.Apartments.CountAsync();
        var unitsOccupied = await Db.Apartments.CountAsync(a => a.Occupancy == ApartmentOccupancy.Occupied);
        var unitsAvailable = await Db.Apartments.CountAsync(a => a.Occupancy == ApartmentOccupancy.Vacant);

        var vm = new DashboardViewModel
        {
            BuildingsTotal = await Db.Buildings.CountAsync(),
            UnitsTotal = unitsTotal,
            UnitsOccupied = unitsOccupied,
            UnitsAvailable = unitsAvailable,
            OccupancyRate = unitsTotal == 0 ? 0 : Math.Round(unitsOccupied * 100.0 / unitsTotal, 1),
            ResidentsTotal = await Db.ApartmentResidents.CountAsync(r => r.MoveOutDate == null),
            MaintenanceOpen = await Db.MaintenanceRequests.CountAsync(m => m.Status == MaintenanceStatus.Open
                || m.Status == MaintenanceStatus.Acknowledged || m.Status == MaintenanceStatus.InProgress),
            RevenueThisMonth = await Db.Payments
                .Where(p => p.Status == PaymentStatus.Succeeded && p.PaidAt >= startOfMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m,
            ArrearsTotal = await Db.Invoices
                .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                .SumAsync(i => (decimal?)i.Balance) ?? 0m,
            InvoicesUnpaid = await Db.Invoices.CountAsync(i =>
                i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
        };

        vm.RecentMaintenance = await Db.MaintenanceRequests
            .Where(m => m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.Acknowledged || m.Status == MaintenanceStatus.InProgress)
            .OrderByDescending(m => m.Priority).ThenByDescending(m => m.ReportedAt)
            .Take(8)
            .Select(m => new RecentMaintenanceRow
            {
                Id = m.Id, RequestNumber = m.RequestNumber, Title = m.Title,
                UnitCode = m.Apartment.UnitCode, Priority = m.Priority, Status = m.Status,
                ReportedAt = m.ReportedAt
            })
            .ToListAsync();

        vm.TopBuildings = await Db.Buildings
            .Select(b => new TopBuildingRow
            {
                Id = b.Id, Name = b.Name,
                Units = b.Apartments.Count(),
                Occupied = b.Apartments.Count(a => a.Occupancy == ApartmentOccupancy.Occupied),
                MonthlyRevenue = b.Apartments.SelectMany(a => a.Invoices)
                    .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                    .Sum(i => (decimal?)i.Balance) ?? 0m
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
        var startOf12Months = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);



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


        // Tỉ lệ thu thành công: AmountPaid / Total trên toàn bộ hoá đơn đã phát hành (loại trừ Draft/Cancelled)
        var invoiceTotals = await Db.Invoices
            .Where(i => i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(i => i.Total), Paid = g.Sum(i => i.AmountPaid) })
            .FirstOrDefaultAsync();
        vm.CollectionRate = invoiceTotals == null || invoiceTotals.Total == 0 ? 0
            : Math.Round((double)(invoiceTotals.Paid * 100m / invoiceTotals.Total), 1);

        // Breakdown doanh thu theo nhóm phí (dựa trên mô tả dòng hoá đơn đã có thu tiền):
        //   "Phí quản lý ..."      → Phí quản lý
        //   dòng có số lượng ≠ 1   → Điện / nước (theo chỉ số)
        //   còn lại                → Gửi xe, phí cố định, phát sinh
        // Tính tỉ lệ trên mỗi hoá đơn rồi nhân với AmountPaid để phân bổ đúng.
        var paidInvoiceItems = await Db.Invoices
            .Where(i => i.AmountPaid > 0)
            .Select(i => new
            {
                i.AmountPaid,
                i.Total,
                Items = i.Items.Select(it => new { it.Description, it.Quantity, it.LineTotal })
            })
            .ToListAsync();
        decimal sumMgmt = 0, sumUtil = 0, sumSvc = 0;
        foreach (var i in paidInvoiceItems)
        {
            if (i.Total <= 0) continue;
            var ratio = i.AmountPaid / i.Total;
            foreach (var it in i.Items)
            {
                var allocated = it.LineTotal * ratio;
                if (it.Description.StartsWith("Phí quản lý")) sumMgmt += allocated;
                else if (it.Quantity != 1m) sumUtil += allocated;
                else sumSvc += allocated;
            }
        }
        vm.BreakdownManagement = Math.Round(sumMgmt, 0);
        vm.BreakdownUtilities = Math.Round(sumUtil, 0);
        vm.BreakdownServices = Math.Round(sumSvc, 0);

        return View(vm);
    }
}
