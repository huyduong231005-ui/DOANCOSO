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

        return View(vm);
    }
}
