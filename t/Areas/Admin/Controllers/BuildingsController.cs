using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

[t.Infrastructure.Security.Permission("projects.view", "projects.manage")]
public class BuildingsController : AdminBaseController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly InvoiceGenerator _invoiceGen;

    public BuildingsController(AppDbContext db, UserManager<AppUser> userManager, InvoiceGenerator invoiceGen) : base(db)
    {
        _userManager = userManager;
        _invoiceGen = invoiceGen;
    }

    // Danh sách toà nằm trong trang Khu; giữ /Admin/Buildings để link cũ không gãy.
    public IActionResult Index() => RedirectToAction("Index", "Projects");

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var rows = await Db.Buildings
            .Where(b => b.Name.Contains(q) || b.Code.Contains(q))
            .OrderBy(b => b.Name)
            .Take(limit)
            .Select(b => new
            {
                title = b.Code + " · " + b.Name,
                subtitle = b.Project != null ? b.Project.Name : "Chưa thuộc khu",
                url = Url.Action(nameof(Details), new { id = b.Id }) ?? "#",
                icon = "domain"
            })
            .ToListAsync();
        return Json(rows);
    }

    /// <summary>Sơ đồ toà: các tầng từ cao xuống thấp, mỗi ô là một căn.</summary>
    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("projects");
        var b = await Db.Buildings
            .Include(x => x.Project).Include(x => x.Manager)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();

        var crumbs = new List<(string, string?)> { ("Khu & toà nhà", Url.Action("Index", "Projects")) };
        if (b.Project != null) crumbs.Add((b.Project.Name, Url.Action("Details", "Projects", new { id = b.ProjectId })));
        crumbs.Add((b.Name, null));
        SetBreadcrumb(crumbs.ToArray());

        var today = VnTime.Today;
        var units = await Db.Apartments
            .Where(a => a.Floor.BuildingId == id)
            .Select(a => new UnitCellVm
            {
                Id = a.Id,
                FloorId = a.FloorId,
                UnitCode = a.UnitCode,
                Area = a.Area,
                Bedrooms = a.Bedrooms,
                Condition = a.Condition,
                IsRented = a.Residents.Any(r => r.MoveOutDate == null && r.Role == ResidentRole.Tenant),
                ResidentCount = a.Residents.Count(r => r.MoveOutDate == null),
                RepresentativeName = a.Residents.Where(r => r.MoveOutDate == null)
                    .OrderByDescending(r => r.IsPrimary).ThenBy(r => r.Role)
                    .Select(r => r.User.FullName).FirstOrDefault(),
                Balance = a.Invoices.Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                    .Sum(i => (decimal?)i.Balance) ?? 0,
                HasOverdue = a.Invoices.Any(i => i.Balance > 0 && i.DueDate < today
                    && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft),
                OpenMaintenance = a.MaintenanceRequests.Count(m => MaintenanceRequest.ActiveStatuses.Contains(m.Status))
            })
            .ToListAsync();

        var floors = await Db.Floors.Where(f => f.BuildingId == id).OrderByDescending(f => f.Number).ToListAsync();
        var vm = new BuildingPlanVm
        {
            Building = b,
            Floors = floors.Select(f => new FloorRowVm
            {
                Floor = f,
                Units = units.Where(u => u.FloorId == f.Id).OrderBy(u => u.UnitCode).ToList()
            }).ToList(),
            ResidentCount = units.Sum(u => u.ResidentCount),
            MonthlyFeeEstimate = Math.Round(units.Where(u => u.Condition != ApartmentCondition.NotHandedOver)
                .Sum(u => (decimal)u.Area) * b.ManagementFeePerM2, 0)
        };
        return View(vm);
    }

    public async Task<IActionResult> Create(int? projectId)
    {
        SetActiveNav("projects");
        SetBreadcrumb(("Khu & toà nhà", Url.Action("Index", "Projects")), ("Thêm toà", null));
        var vm = new BuildingEditVm
        {
            Status = BuildingStatus.Active, FloorCount = 20, UnitsPerFloor = 8,
            ProjectId = projectId, ManagementFeePerM2 = 15000
        };
        await PopulateLookupsAsync(vm);
        return View("Edit", vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        SetActiveNav("projects");
        var b = await Db.Buildings.Include(x => x.Floors).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        SetBreadcrumb(("Khu & toà nhà", Url.Action("Index", "Projects")), (b.Name, Url.Action(nameof(Details), new { id })), ("Sửa", null));

        var vm = new BuildingEditVm
        {
            Id = b.Id, Name = b.Name, Code = b.Code,
            ProjectId = b.ProjectId,
            FloorCount = b.Floors.Count,
            ThumbnailUrl = b.ThumbnailUrl, Description = b.Description,
            ManagerId = b.ManagerId, Status = b.Status,
            DefaultBillingDay = b.DefaultBillingDay,
            DefaultLateFeeAfterDays = b.DefaultLateFeeAfterDays,
            DefaultLateFeePercent = b.DefaultLateFeePercent,
            ManagementFeePerM2 = b.ManagementFeePerM2
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

        Building b;
        bool isNew = input.Id == 0;
        if (isNew)
        {
            b = new Building();
            Db.Buildings.Add(b);
        }
        else
        {
            if ((b = (await Db.Buildings.FirstOrDefaultAsync(x => x.Id == input.Id))!) is null) return NotFound(); // Id gửi lên không tồn tại
        }

        b.Name = input.Name; b.Code = input.Code?.Trim() ?? string.Empty;
        b.ProjectId = input.ProjectId!.Value; // [Required] đã kiểm tra
        b.ThumbnailUrl = input.ThumbnailUrl; b.Description = input.Description;
        b.ManagerId = string.IsNullOrEmpty(input.ManagerId) ? null : input.ManagerId;
        b.Status = input.Status;
        b.DefaultBillingDay = input.DefaultBillingDay;
        b.DefaultLateFeeAfterDays = input.DefaultLateFeeAfterDays;
        b.DefaultLateFeePercent = input.DefaultLateFeePercent;
        b.ManagementFeePerM2 = input.ManagementFeePerM2;
        await Db.SaveChangesAsync();

        // Thêm tầng còn thiếu khi tăng số tầng (không tự xoá tầng khi giảm để tránh mất căn).
        var existing = await Db.Floors.Where(f => f.BuildingId == b.Id).Select(f => f.Number).ToListAsync();
        var newFloors = Enumerable.Range(1, input.FloorCount).Except(existing)
            .Select(n => new Floor { BuildingId = b.Id, Number = n }).ToList();
        Db.Floors.AddRange(newFloors);
        await Db.SaveChangesAsync();

        if (isNew && input.UnitsPerFloor > 0)
        {
            var prefix = string.IsNullOrEmpty(b.Code) ? "" : b.Code + "-";
            foreach (var f in newFloors)
                for (var u = 1; u <= input.UnitsPerFloor; u++)
                    Db.Apartments.Add(new Apartment
                    {
                        FloorId = f.Id,
                        UnitCode = $"{prefix}{f.Number:00}.{u:00}",
                        Area = input.DefaultUnitArea, Bedrooms = 2, Bathrooms = 2,
                        Condition = ApartmentCondition.NotHandedOver
                    });
            await Db.SaveChangesAsync();
        }

        TempData["Success"] = isNew
            ? $"Đã tạo toà {b.Name}" + (input.UnitsPerFloor > 0 ? $" với {newFloors.Count * input.UnitsPerFloor} căn." : ".")
            : "Đã cập nhật toà nhà.";
        return RedirectToAction(nameof(Details), new { id = b.Id });
    }

    /// <summary>Phát hành hoá đơn phí tháng hiện tại cho mọi căn đã bàn giao của toà.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [t.Infrastructure.Security.Permission("invoices.issue")]
    public async Task<IActionResult> GenerateInvoices(int id)
    {
        if (!await Db.Buildings.AnyAsync(b => b.Id == id)) return NotFound();
        var count = await _invoiceGen.GenerateForBuildingAsync(id, VnTime.Today);
        TempData[count > 0 ? "Success" : "Info"] = count > 0
            ? $"Đã phát hành {count} hoá đơn phí tháng {VnTime.Today:MM/yyyy}."
            : "Không có hoá đơn mới (các căn đã có hoá đơn tháng này hoặc chưa có khoản phí).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        if (await Db.Apartments.AnyAsync(a => a.Floor.BuildingId == id))
        {
            TempData["Danger"] = "Không thể xoá toà nhà còn căn hộ.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var projectId = b.ProjectId;
        Db.Buildings.Remove(b);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá toà nhà.";
        return RedirectToAction("Details", "Projects", new { id = projectId });
    }

    private async Task PopulateLookupsAsync(BuildingEditVm vm)
    {
        vm.Projects = await Db.Projects.OrderBy(p => p.Name).ToListAsync();
        var staff = (await _userManager.GetUsersInRoleAsync("Admin"))
            .Concat(await _userManager.GetUsersInRoleAsync("Manager"))
            .DistinctBy(u => u.Id)
            .OrderBy(u => u.FullName);
        vm.Managers = staff.Select(u => new HostOption { Id = u.Id, Label = u.FullName + " · " + u.Email }).ToList();
    }
}
