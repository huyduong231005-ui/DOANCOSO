using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

/// <summary>Khu căn hộ — cấp cao nhất của cây Khu → Toà → Tầng → Căn.</summary>
[t.Infrastructure.Security.Permission("projects.view", "projects.manage")]
public class ProjectsController : AdminBaseController
{
    public ProjectsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q)
    {
        SetActiveNav("projects");
        SetBreadcrumb(("Khu & toà nhà", null));

        var query = Db.Projects.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || p.Address.Contains(q));

        var rows = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProjectListVm
            {
                Id = p.Id, Name = p.Name,
                RegionName = p.Region.Name, Address = p.Address, ThumbnailUrl = p.ThumbnailUrl,
                BuildingCount = p.Buildings.Count(),
                ApartmentCount = p.Buildings.SelectMany(b => b.Floors.SelectMany(f => f.Apartments)).Count(),
                OccupiedCount = p.Buildings.SelectMany(b => b.Floors.SelectMany(f => f.Apartments)).AsQueryable().Count(Apartment.IsOccupied),
                ResidentCount = p.Buildings.SelectMany(b => b.Floors.SelectMany(f => f.Apartments)).SelectMany(a => a.Residents).Count(r => r.MoveOutDate == null),
                Outstanding = p.Buildings.SelectMany(b => b.Floors.SelectMany(f => f.Apartments)).SelectMany(a => a.Invoices)
                    .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                    .Sum(i => (decimal?)i.Balance) ?? 0
            })
            .ToListAsync();

        ViewBag.Q = q;
        return View(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("projects");
        var project = await Db.Projects.Include(p => p.Region).FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();
        SetBreadcrumb(("Khu & toà nhà", Url.Action(nameof(Index))), (project.Name, null));

        ViewBag.Buildings = await BuildingCardsAsync(Db.Buildings.Where(b => b.ProjectId == id));
        return View(project);
    }

    private Task<List<BuildingListVm>> BuildingCardsAsync(IQueryable<Building> buildings) =>
        buildings
            .OrderBy(b => b.Code).ThenBy(b => b.Name)
            .Select(b => new BuildingListVm
            {
                Id = b.Id, Name = b.Name, Code = b.Code,
                ThumbnailUrl = b.ThumbnailUrl,
                FloorCount = b.Floors.Count(),
                ManagerName = b.Manager != null ? b.Manager.FullName : null,
                Status = b.Status,
                UnitsTotal = b.Floors.SelectMany(f => f.Apartments).Count(),
                UnitsOccupied = b.Floors.SelectMany(f => f.Apartments).AsQueryable().Count(Apartment.IsOccupied),
                UnitsVacant = b.Floors.SelectMany(f => f.Apartments).AsQueryable().Count(Apartment.IsVacant),
                Outstanding = b.Floors.SelectMany(f => f.Apartments).SelectMany(a => a.Invoices)
                    .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                    .Sum(i => (decimal?)i.Balance) ?? 0,
                OpenMaintenance = Db.MaintenanceRequests.Count(m =>
                    ((m.Apartment != null && m.Apartment.Floor.BuildingId == b.Id) || m.CommonBuildingId == b.Id)
                    && MaintenanceRequest.ActiveStatuses.Contains(m.Status))
            })
            .ToListAsync();

    public async Task<IActionResult> Create()
    {
        SetActiveNav("projects");
        SetBreadcrumb(("Khu & toà nhà", Url.Action(nameof(Index))), ("Thêm khu", null));
        var vm = new ProjectEditVm { Regions = await Db.Regions.OrderBy(r => r.Name).ToListAsync() };
        return View("Edit", vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        SetActiveNav("projects");
        var p = await Db.Projects.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        SetBreadcrumb(("Khu & toà nhà", Url.Action(nameof(Index))), (p.Name, Url.Action(nameof(Details), new { id })), ("Sửa", null));

        var vm = new ProjectEditVm
        {
            Id = p.Id, Name = p.Name, RegionId = p.RegionId,
            Address = p.Address, ThumbnailUrl = p.ThumbnailUrl, ShortDescription = p.ShortDescription,
            Regions = await Db.Regions.OrderBy(r => r.Name).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectEditVm input)
    {
        if (!ModelState.IsValid)
        {
            input.Regions = await Db.Regions.OrderBy(r => r.Name).ToListAsync();
            return View(input);
        }

        Project p;
        if (input.Id == 0)
        {
            p = new Project();
            Db.Projects.Add(p);
        }
        else
        {
            if ((p = (await Db.Projects.FirstOrDefaultAsync(x => x.Id == input.Id))!) is null) return NotFound(); // Id gửi lên không tồn tại
        }

        p.Name = input.Name; p.RegionId = input.RegionId;
        p.Address = input.Address; p.ThumbnailUrl = input.ThumbnailUrl ?? string.Empty;
        p.ShortDescription = input.ShortDescription ?? string.Empty;

        await Db.SaveChangesAsync();
        TempData["Success"] = input.Id == 0 ? "Đã tạo khu." : "Đã cập nhật khu.";
        return RedirectToAction(nameof(Details), new { id = p.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await Db.Projects.Include(x => x.Buildings).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (p.Buildings.Any())
        {
            TempData["Danger"] = "Không thể xoá khu còn toà nhà.";
            return RedirectToAction(nameof(Details), new { id });
        }
        Db.Projects.Remove(p);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá khu.";
        return RedirectToAction(nameof(Index));
    }
}
