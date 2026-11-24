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
        // Mốc tháng/ngày theo giờ Việt Nam; PaidAt lưu UTC nên mốc được đổi sang UTC trước khi so.
        var today = t.Infrastructure.Time.VnTime.Today;
        var startOfMonthVn = new DateTime(today.Year, today.Month, 1);
        // Thực thu = đã trả trừ đã hoàn, gồm cả thanh toán đã hoàn một phần; gom theo ngày Việt Nam ở bộ nhớ.
        var startOf12MonthsVn = startOfMonthVn.AddMonths(-11);
        var collected = (await Db.Payments
                .Where(p => (p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.PartiallyRefunded)
                            && p.PaidAt != null && p.PaidAt >= t.Infrastructure.Time.VnTime.ToUtc(startOf12MonthsVn))
                .Select(p => new { p.PaidAt, Net = p.Amount - p.RefundedAmount })
                .ToListAsync())
            .Select(p => (Day: t.Infrastructure.Time.VnTime.FromUtc(p.PaidAt!.Value).Date, p.Net))
            .ToList();

        var unitsTotal = await Db.Floors.SelectMany(f => f.Apartments).CountAsync();
        var unitsOccupied = await Db.Floors.SelectMany(f => f.Apartments).CountAsync(Apartment.IsOccupied);
        var unitsAvailable = await Db.Floors.SelectMany(f => f.Apartments).CountAsync(Apartment.IsVacant);

        var vm = new DashboardViewModel
        {
            BuildingsTotal = await Db.Buildings.CountAsync(),
            UnitsTotal = unitsTotal,
            UnitsOccupied = unitsOccupied,
            UnitsAvailable = unitsAvailable,
            OccupancyRate = unitsTotal == 0 ? 0 : Math.Round(unitsOccupied * 100.0 / unitsTotal, 1),
            ResidentsTotal = await Db.ApartmentResidents.CountAsync(r => r.MoveOutDate == null),
            MaintenanceOpen = await Db.MaintenanceRequests.CountAsync(m => MaintenanceRequest.ActiveStatuses.Contains(m.Status)),
            MaintenanceOverdue = await Db.MaintenanceRequests.CountAsync(t.Infrastructure.Maintenance.MaintenanceWorkflow.IsOverdue(now)),
            RevenueThisMonth = collected.Where(p => p.Day >= startOfMonthVn).Sum(p => p.Net),
            ArrearsTotal = await Db.Invoices
                .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                .SumAsync(i => (decimal?)i.Balance) ?? 0m,
            InvoicesUnpaid = await Db.Invoices.CountAsync(i =>
                i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
        };

        vm.RecentMaintenance = await Db.MaintenanceRequests
            .Where(m => MaintenanceRequest.ActiveStatuses.Contains(m.Status))
            .OrderByDescending(m => m.Priority).ThenByDescending(m => m.ReportedAt)
            .Take(8)
            .Select(m => new RecentMaintenanceRow
            {
                Id = m.Id, RequestNumber = m.RequestNumber, Title = m.Title,
                UnitCode = m.Apartment != null ? m.Apartment.UnitCode : "Chung: " + m.Location, Priority = m.Priority, Status = m.Status,
                ReportedAt = m.ReportedAt
            })
            .ToListAsync();

        vm.TopBuildings = await Db.Buildings
            .Select(b => new TopBuildingRow
            {
                Id = b.Id, Name = b.Name,
                Units = b.Floors.SelectMany(f => f.Apartments).Count(),
                Occupied = b.Floors.SelectMany(f => f.Apartments).AsQueryable().Count(Apartment.IsOccupied),
                MonthlyRevenue = b.Floors.SelectMany(f => f.Apartments).SelectMany(a => a.Invoices)
                    .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                    .Sum(i => (decimal?)i.Balance) ?? 0m
            })
            .OrderByDescending(x => x.MonthlyRevenue)
            .Take(5)
            .ToListAsync();

        var startOfWindow = today.AddDays(-13);
        var revenueRaw = collected.Where(p => p.Day >= startOfWindow).GroupBy(p => p.Day)
            .Select(g => new RevenueByDayRow { Day = g.Key, Total = g.Sum(p => p.Net) })
            .ToList();

        vm.RevenueByDay = Enumerable.Range(0, 14)
            .Select(i => startOfWindow.AddDays(i))
            .Select(day => revenueRaw.FirstOrDefault(r => r.Day == day) ?? new RevenueByDayRow { Day = day, Total = 0 })
            .ToList();

        // ─────────────────────────────────────────────────────────────
        // PHÂN TÍCH TÀI CHÍNH NÂNG CAO
        // ─────────────────────────────────────────────────────────────
        var startOfLastMonthVn = startOfMonthVn.AddMonths(-1);
        var startOf12Months = startOf12MonthsVn;



        vm.RevenueLastMonth = collected.Where(p => p.Day >= startOfLastMonthVn && p.Day < startOfMonthVn).Sum(p => p.Net);

        vm.MoMGrowthPct = vm.RevenueLastMonth == 0 ? 0
            : Math.Round((double)((vm.RevenueThisMonth - vm.RevenueLastMonth) * 100m / vm.RevenueLastMonth), 1);

        // Doanh thu trung bình 12 tháng (chỉ tính các tháng có dữ liệu)
        var revenueByMonthRaw = collected.GroupBy(p => new { p.Day.Year, p.Day.Month })
            .Select(g => new RevenueByMonthRow { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(p => p.Net) })
            .ToList();

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
