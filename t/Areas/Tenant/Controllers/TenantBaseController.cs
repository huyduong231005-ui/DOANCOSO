using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Tenant.Controllers;

[Area("Tenant")]
[Authorize(Roles = "Tenant,Admin,Manager")]
public abstract class TenantBaseController : Controller
{
    protected readonly AppDbContext Db;

    protected TenantBaseController(AppDbContext db) => Db = db;

    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected void SetBreadcrumb(params (string Text, string? Url)[] crumbs)
    {
        ViewData["Breadcrumbs"] = crumbs.ToList();
    }

    protected void SetActiveNav(string key) => ViewData["ActiveNav"] = key;

    /// <summary>Lease where the current user is the primary tenant or co-tenant.</summary>
    protected IQueryable<Lease> MyLeasesQuery()
    {
        var uid = CurrentUserId;
        return Db.Leases.Where(l =>
            l.PrimaryTenantId == uid ||
            l.AdditionalTenants.Any(t => t.TenantId == uid));
    }

    protected Task<Lease?> GetActiveLeaseAsync() =>
        MyLeasesQuery()
            .Where(l => l.Status == LeaseStatus.Active)
            .OrderByDescending(l => l.StartDate)
            .FirstOrDefaultAsync();
}
