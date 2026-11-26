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

    /// <summary>Id các căn mà người dùng hiện tại đang cư trú (tài khoản được BQL liên kết).</summary>
    protected IQueryable<int> MyApartmentIds()
    {
        var uid = CurrentUserId;
        return Db.ApartmentResidents
            .Where(r => r.UserId == uid && r.MoveOutDate == null && r.ConfirmedAt != null)
            .Select(r => r.ApartmentId);
    }

    protected Task<Apartment?> GetMyApartmentAsync() =>
        Db.Apartments
            .Include(a => a.Building).ThenInclude(b => b.Project)
            .Where(a => MyApartmentIds().Contains(a.Id))
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync();
}
