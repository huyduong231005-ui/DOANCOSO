using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Data;

namespace t.Areas.Tenant.Controllers;

public class UtilitiesController : TenantBaseController
{
    public UtilitiesController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index()
    {
        SetActiveNav("utilities");
        SetBreadcrumb(("Điện / Nước", null));

        var leaseIds = await MyLeasesQuery().Select(l => l.Id).ToListAsync();
        var rows = await Db.UtilityReadings
            .Include(r => r.UtilityType)
            .Include(r => r.Lease)
            .Where(r => leaseIds.Contains(r.LeaseId))
            .OrderByDescending(r => r.BillingMonth).ThenBy(r => r.UtilityType.Name)
            .ToListAsync();
        return View(rows);
    }
}
