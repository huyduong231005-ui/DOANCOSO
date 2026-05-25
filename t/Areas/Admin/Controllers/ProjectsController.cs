using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Localization;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class ProjectsController : AdminBaseController
{
    public ProjectsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, ProjectStatus? status, int page = 1)
    {
        SetActiveNav("projects");
        SetBreadcrumb(("Dự án", null));

        const int pageSize = 15;
        var query = Db.Projects.Include(p => p.Region).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || p.Slug.Contains(q));
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProjectListVm
            {
                Id = p.Id, Name = p.Name, Slug = p.Slug,
                RegionName = p.Region.Name, Status = p.Status,
                PriceFrom = p.PriceFrom,
                ApartmentCount = p.Apartments.Count(),
                CreatedAt = p.CreatedAt
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
        var raw = await Db.Projects
            .Include(p => p.Region)
            .Where(p => p.Name.Contains(q) || p.Slug.Contains(q))
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new { p.Id, p.Name, p.ThumbnailUrl, RegionName = p.Region.Name, p.Status })
            .ToListAsync();
        return Json(raw.Select(p => new
        {
            title = p.Name,
            subtitle = p.RegionName + " · " + p.Status.Vi(),
            url = Url.Action(nameof(Edit), new { id = p.Id }) ?? "#",
            thumb = p.ThumbnailUrl,
            icon = "location_city"
        }));
    }

    public async Task<IActionResult> Create()
    {
        SetActiveNav("projects");
        SetBreadcrumb(("Dự án", Url.Action(nameof(Index))), ("Thêm mới", null));
        var vm = new ProjectEditVm { Status = ProjectStatus.OpenForRent };
        vm.Regions = await Db.Regions.OrderBy(r => r.Name).ToListAsync();
        return View("Edit", vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        SetActiveNav("projects");
        var p = await Db.Projects.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        SetBreadcrumb(("Dự án", Url.Action(nameof(Index))), (p.Name, null));

        var vm = new ProjectEditVm
        {
            Id = p.Id, Name = p.Name, Slug = p.Slug, RegionId = p.RegionId,
            Address = p.Address, ThumbnailUrl = p.ThumbnailUrl, PriceFrom = p.PriceFrom,
            Status = p.Status, ShortDescription = p.ShortDescription, FullDescription = p.FullDescription,
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

        if (string.IsNullOrWhiteSpace(input.Slug)) input.Slug = Slugify(input.Name);

        if (await Db.Projects.AnyAsync(p => p.Slug == input.Slug && p.Id != input.Id))
        {
            ModelState.AddModelError(nameof(input.Slug), "Slug đã tồn tại.");
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
            p = await Db.Projects.FirstOrDefaultAsync(x => x.Id == input.Id) ?? throw new InvalidOperationException("Not found");
        }

        p.Name = input.Name; p.Slug = input.Slug; p.RegionId = input.RegionId;
        p.Address = input.Address; p.ThumbnailUrl = input.ThumbnailUrl;
        p.PriceFrom = input.PriceFrom; p.Status = input.Status;
        p.ShortDescription = input.ShortDescription; p.FullDescription = input.FullDescription;

        await Db.SaveChangesAsync();
        TempData["Success"] = input.Id == 0 ? "Đã tạo dự án." : "Đã cập nhật dự án.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await Db.Projects.Include(x => x.Apartments).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (p.Apartments.Any())
        {
            TempData["Danger"] = "Không thể xoá dự án còn căn hộ liên kết.";
            return RedirectToAction(nameof(Index));
        }
        Db.Projects.Remove(p);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá dự án.";
        return RedirectToAction(nameof(Index));
    }
}
