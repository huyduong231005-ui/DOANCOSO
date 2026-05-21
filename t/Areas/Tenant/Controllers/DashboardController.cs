using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Tenant.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Tenant.Controllers;

public class DashboardController : TenantBaseController
{
    public DashboardController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index()
    {
        SetActiveNav("dashboard");
        SetBreadcrumb(("Trang chính", null));

        var uid = CurrentUserId;
        var lease = await MyLeasesQuery()
            .Include(l => l.Apartment).ThenInclude(a => a.Building)
            .Include(l => l.Apartment).ThenInclude(a => a.Floor)
            .OrderByDescending(l => l.Status == LeaseStatus.Active ? 1 : 0)
            .ThenByDescending(l => l.StartDate)
            .FirstOrDefaultAsync();

        var leaseIds = await MyLeasesQuery().Select(l => l.Id).ToListAsync();

        var unpaidInvoices = await Db.Invoices
            .Where(i => leaseIds.Contains(i.LeaseId) &&
                       (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue))
            .ToListAsync();

        var vm = new TenantDashboardVm
        {
            ActiveLease = lease,
            InvoicesUnpaidCount = unpaidInvoices.Count,
            TotalDue = unpaidInvoices.Sum(i => i.Balance),
            NextDueDate = unpaidInvoices.OrderBy(i => i.DueDate).Select(i => (DateTime?)i.DueDate).FirstOrDefault(),
            OpenMaintenanceCount = await Db.MaintenanceRequests
                .CountAsync(m => m.ReporterId == uid && (m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.Acknowledged || m.Status == MaintenanceStatus.InProgress))
        };

        vm.RecentInvoices = await Db.Invoices
            .Where(i => leaseIds.Contains(i.LeaseId))
            .OrderByDescending(i => i.IssueDate)
            .Take(5)
            .Select(i => new TenantInvoiceRow
            {
                Id = i.Id, InvoiceNumber = i.InvoiceNumber,
                Kind = i.Kind, BillingMonth = i.BillingMonth,
                DueDate = i.DueDate, Total = i.Total, Balance = i.Balance, Status = i.Status
            })
            .ToListAsync();

        vm.RecentMaintenance = await Db.MaintenanceRequests
            .Where(m => m.ReporterId == uid)
            .OrderByDescending(m => m.ReportedAt)
            .Take(5)
            .Select(m => new TenantMaintenanceRow
            {
                Id = m.Id, RequestNumber = m.RequestNumber, Title = m.Title,
                Category = m.Category, Priority = m.Priority, Status = m.Status,
                ReportedAt = m.ReportedAt
            })
            .ToListAsync();

        return View(vm);
    }
}
