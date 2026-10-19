using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

/// <summary>Danh bạ cư dân đang ở. Thêm/chuyển đi thực hiện trong trang chi tiết căn.</summary>
public class ResidentsController : AdminBaseController
{
    public ResidentsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, int? buildingId, ResidentRole? role, int page = 1)
    {
        SetActiveNav("residents");
        SetBreadcrumb(("Cư dân", null));

        const int pageSize = 30;
        var query = Db.ApartmentResidents.Where(r => r.MoveOutDate == null);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(r => r.FullName.Contains(q) || (r.Phone != null && r.Phone.Contains(q))
                                     || (r.Email != null && r.Email.Contains(q)) || r.Apartment.UnitCode.Contains(q));
        if (buildingId.HasValue) query = query.Where(r => r.Apartment.BuildingId == buildingId);
        if (role.HasValue) query = query.Where(r => r.Role == role);

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(r => r.Apartment.Building.Code).ThenBy(r => r.Apartment.UnitCode)
            .ThenByDescending(r => r.IsPrimary).ThenBy(r => r.Role)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new ResidentListVm
            {
                Id = r.Id, ApartmentId = r.ApartmentId,
                FullName = r.FullName, Phone = r.Phone, Email = r.Email,
                Role = r.Role, IsPrimary = r.IsPrimary, HasAccount = r.UserId != null,
                UnitCode = r.Apartment.UnitCode, BuildingName = r.Apartment.Building.Name,
                MoveInDate = r.MoveInDate
            })
            .ToListAsync();

        ViewBag.Buildings = await Db.Buildings.OrderBy(b => b.Code).ToListAsync();
        ViewBag.Q = q; ViewBag.BuildingId = buildingId; ViewBag.Role = role;
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = p => Url.Action(nameof(Index), new { q, buildingId, role, page = p }) ?? "#"
        };
        return View(rows);
    }
}
