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
    private readonly InvoiceGenerator _invoiceGen;

    public BuildingsController(AppDbContext db, InvoiceGenerator invoiceGen) : base(db)
    {
        _invoiceGen = invoiceGen;
    }

    // Danh sách toà nằm trong trang Khu; giữ /Admin/Buildings để link cũ không gãy.
    public IActionResult Index() => RedirectToAction("Index", "Projects");

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var rows = await Db.Buildings
            .Where(b => b.Code.Contains(q))
            .OrderBy(b => b.Code)
            .Take(limit)
            .Select(b => new
            {
                title = "Toà " + b.Code,
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
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();

        var crumbs = new List<(string, string?)> { ("Khu & toà nhà", Url.Action("Index", "Projects")) };
        if (b.Project != null) crumbs.Add((b.Project.Name, Url.Action("Details", "Projects", new { id = b.ProjectId })));
        crumbs.Add((b.Name, null));
        SetBreadcrumb(crumbs.ToArray());

        var today = VnTime.Today;
        var units = await Db.Apartments
            .Where(a => a.BuildingId == id)
            .Select(a => new UnitCellVm
            {
                Id = a.Id,
                FloorNumber = a.FloorNumber,
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

        var feePerM2 = await Db.UtilityTypes.Where(t => t.Method == BillingMethod.ByArea).SumAsync(t => t.DefaultRate);
        var vm = new BuildingPlanVm
        {
            Building = b,
            // Mỗi hàng của sơ đồ là một tầng có căn, tầng cao ở trên.
            Floors = units.GroupBy(u => u.FloorNumber).OrderByDescending(g => g.Key)
                .Select(g => new FloorRowVm { Number = g.Key, Units = g.OrderBy(u => u.UnitCode).ToList() }).ToList(),
            ResidentCount = units.Sum(u => u.ResidentCount),
            MonthlyFeeEstimate = Math.Round(units.Where(u => u.Condition != ApartmentCondition.NotHandedOver)
                .Sum(u => (decimal)u.Area) * feePerM2, 0),
            ManagementFeePerM2 = feePerM2
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
            ProjectId = projectId
        };
        await PopulateLookupsAsync(vm);
        return View("Edit", vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        SetActiveNav("projects");
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        SetBreadcrumb(("Khu & toà nhà", Url.Action("Index", "Projects")), (b.Name, Url.Action(nameof(Details), new { id })), ("Sửa", null));

        var vm = new BuildingEditVm
        {
            Id = b.Id, Code = b.Code,
            ProjectId = b.ProjectId,
            Status = b.Status,
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

        b.Code = input.Code?.Trim() ?? string.Empty;
        b.ProjectId = input.ProjectId!.Value; // [Required] đã kiểm tra
        b.Status = input.Status;
        b.DefaultBillingDay = input.DefaultBillingDay;
        b.DefaultLateFeeAfterDays = input.DefaultLateFeeAfterDays;
        b.DefaultLateFeePercent = input.DefaultLateFeePercent;
        await Db.SaveChangesAsync();

        // Toà mới: sinh sẵn căn cho từng tầng (tầng không thành bảng, mỗi căn ghi số tầng).
        if (isNew && input.UnitsPerFloor > 0)
        {
            var prefix = string.IsNullOrEmpty(b.Code) ? "" : b.Code + "-";
            for (var f = 1; f <= input.FloorCount; f++)
                for (var u = 1; u <= input.UnitsPerFloor; u++)
                    Db.Apartments.Add(new Apartment
                    {
                        BuildingId = b.Id, FloorNumber = f,
                        UnitCode = $"{prefix}{f:00}.{u:00}",
                        Area = input.DefaultUnitArea, Bedrooms = 2,
                        Condition = ApartmentCondition.NotHandedOver
                    });
            await Db.SaveChangesAsync();
        }

        TempData["Success"] = isNew
            ? $"Đã tạo toà {b.Name}" + (input.UnitsPerFloor > 0 ? $" với {input.FloorCount * input.UnitsPerFloor} căn." : ".")
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
        if (await Db.Apartments.AnyAsync(a => a.BuildingId == id))
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
    }
}
