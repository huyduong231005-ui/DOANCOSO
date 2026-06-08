using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class BuildingsController : AdminBaseController
{
    private readonly UserManager<AppUser> _userManager;

    public BuildingsController(AppDbContext db, UserManager<AppUser> userManager) : base(db)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? q, BuildingStatus? status, int page = 1)
    {
        SetActiveNav("buildings");
        SetBreadcrumb(("Toà nhà", null));

        const int pageSize = 20;
        var query = Db.Buildings.Include(b => b.Region).Include(b => b.Project).Include(b => b.Manager).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(b => b.Name.Contains(q) || b.Code.Contains(q) || b.Slug.Contains(q));
        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new BuildingListVm
            {
                Id = b.Id, Name = b.Name, Code = b.Code,
                RegionName = b.Region.Name,
                ProjectName = b.Project != null ? b.Project.Name : null,
                ManagerName = b.Manager != null ? b.Manager.FullName : null,
                Status = b.Status,
                UnitsTotal = b.Apartments.Count(),
                UnitsOccupied = b.Apartments.Count(a => a.Occupancy == ApartmentOccupancy.Occupied),
                CreatedAt = b.CreatedAt
            }).ToListAsync();

        ViewBag.Q = q; ViewBag.Status = status;
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { q, status, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var rows = await Db.Buildings
            .Include(b => b.Region)
            .Where(b => b.Name.Contains(q) || b.Code.Contains(q) || b.Slug.Contains(q))
            .OrderBy(b => b.Name)
            .Take(limit)
            .Select(b => new
            {
                title = b.Code + " · " + b.Name,
                subtitle = b.Region.Name,
                url = Url.Action(nameof(Details), new { id = b.Id }) ?? "#",
                icon = "domain"
            })
            .ToListAsync();
        return Json(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("buildings");
        var b = await Db.Buildings
            .Include(x => x.Region).Include(x => x.Project).Include(x => x.Manager)
            .Include(x => x.Floors)
            .Include(x => x.Apartments).ThenInclude(a => a.Floor)
            .Include(x => x.Apartments).ThenInclude(a => a.Leases)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        SetBreadcrumb(("Toà nhà", Url.Action(nameof(Index))), (b.Name, null));
        return View(b);
    }

    public async Task<IActionResult> Create()
    {
        SetActiveNav("buildings");
        SetBreadcrumb(("Toà nhà", Url.Action(nameof(Index))), ("Thêm mới", null));
        var vm = new BuildingEditVm { Status = BuildingStatus.Active, FloorCount = 1 };
        await PopulateLookupsAsync(vm);
        return View("Edit", vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        SetActiveNav("buildings");
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        SetBreadcrumb(("Toà nhà", Url.Action(nameof(Index))), (b.Name, null));

        var vm = new BuildingEditVm
        {
            Id = b.Id, Name = b.Name, Slug = b.Slug, Code = b.Code,
            ProjectId = b.ProjectId, RegionId = b.RegionId,
            Address = b.Address, FloorCount = b.FloorCount,
            ThumbnailUrl = b.ThumbnailUrl, Description = b.Description,
            ManagerId = b.ManagerId, Status = b.Status,
            DefaultBillingDay = b.DefaultBillingDay,
            DefaultLateFeeAfterDays = b.DefaultLateFeeAfterDays,
            DefaultLateFeePercent = b.DefaultLateFeePercent
        };
        await PopulateLookupsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BuildingEditVm input)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(input);
            return View(input);
        }
        if (string.IsNullOrWhiteSpace(input.Slug)) input.Slug = Slugify(input.Name);
        if (await Db.Buildings.AnyAsync(b => b.Slug == input.Slug && b.Id != input.Id))
        {
            ModelState.AddModelError(nameof(input.Slug), "Slug đã tồn tại.");
            await PopulateLookupsAsync(input);
            return View(input);
        }

        Building b;
        bool isNew = input.Id == 0;
        if (isNew)
        {
            b = new Building();
            Db.Buildings.Add(b);
        }
        else
        {
            b = await Db.Buildings.Include(x => x.Floors).FirstOrDefaultAsync(x => x.Id == input.Id) ?? throw new InvalidOperationException();
        }

        b.Name = input.Name; b.Slug = input.Slug; b.Code = input.Code;
        b.ProjectId = input.ProjectId; b.RegionId = input.RegionId;
        b.Address = input.Address; b.FloorCount = input.FloorCount;
        b.ThumbnailUrl = input.ThumbnailUrl; b.Description = input.Description;
        b.ManagerId = string.IsNullOrEmpty(input.ManagerId) ? null : input.ManagerId;
        b.Status = input.Status;
        b.DefaultBillingDay = input.DefaultBillingDay;
        b.DefaultLateFeeAfterDays = input.DefaultLateFeeAfterDays;
        b.DefaultLateFeePercent = input.DefaultLateFeePercent;

        await Db.SaveChangesAsync();

        if (isNew)
        {
            for (int i = 1; i <= input.FloorCount; i++)
                Db.Floors.Add(new Floor { BuildingId = b.Id, Number = i, Label = $"Tầng {i}" });
            await Db.SaveChangesAsync();
        }

        TempData["Success"] = isNew ? "Đã tạo toà nhà." : "Đã cập nhật toà nhà.";
        return RedirectToAction(nameof(Details), new { id = b.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await Db.Buildings.Include(x => x.Apartments).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        if (b.Apartments.Any())
        {
            TempData["Danger"] = "Không thể xoá toà nhà còn căn hộ liên kết.";
            return RedirectToAction(nameof(Index));
        }
        Db.Buildings.Remove(b);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá toà nhà.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(BuildingEditVm vm)
    {
        vm.Regions = await Db.Regions.OrderBy(r => r.Name).ToListAsync();
        vm.Projects = await Db.Projects.OrderBy(p => p.Name).ToListAsync();
        vm.Managers = await _userManager.Users.OrderBy(u => u.FullName)
            .Select(u => new HostOption { Id = u.Id, Label = u.FullName + " · " + u.Email })
            .ToListAsync();
    }
}
