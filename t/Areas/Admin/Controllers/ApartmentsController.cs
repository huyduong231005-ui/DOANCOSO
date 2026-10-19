using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Localization;
using t.Infrastructure.Residents;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class ApartmentsController : AdminBaseController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly InvoiceGenerator _invoiceGen;
    private readonly ResidentInvites _invites;

    public ApartmentsController(AppDbContext db, UserManager<AppUser> userManager, InvoiceGenerator invoiceGen, ResidentInvites invites) : base(db)
    {
        _userManager = userManager;
        _invoiceGen = invoiceGen;
        _invites = invites;
    }

    public async Task<IActionResult> Index(string? q, ApartmentOccupancy? occupancy, int? projectId, int? buildingId, bool debt = false, int page = 1)
    {
        SetActiveNav("apartments");
        SetBreadcrumb(("Căn hộ", null));

        const int pageSize = 25;
        var today = VnTime.Today;
        var query = Db.Apartments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => a.UnitCode.Contains(q) || a.Residents.Any(r => r.MoveOutDate == null &&
                (r.FullName.Contains(q) || (r.Phone != null && r.Phone.Contains(q)))));
        if (occupancy.HasValue) query = query.Where(a => a.Occupancy == occupancy.Value);
        if (projectId.HasValue) query = query.Where(a => a.Building.ProjectId == projectId.Value);
        if (buildingId.HasValue) query = query.Where(a => a.BuildingId == buildingId.Value);
        if (debt) query = query.Where(a => a.Invoices.Any(i => i.Balance > 0 && i.DueDate < today
            && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft));

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(a => a.Building.Code).ThenBy(a => a.Floor.Number).ThenBy(a => a.UnitCode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ApartmentListVm
            {
                Id = a.Id,
                UnitCode = a.UnitCode,
                BuildingName = a.Building.Name,
                ProjectName = a.Building.Project != null ? a.Building.Project.Name : "",
                FloorNumber = a.Floor.Number,
                Area = a.Area,
                Bedrooms = a.Bedrooms,
                Occupancy = a.Occupancy,
                RepresentativeName = a.Residents.Where(r => r.MoveOutDate == null)
                    .OrderByDescending(r => r.IsPrimary).ThenBy(r => r.Role).Select(r => r.FullName).FirstOrDefault(),
                ResidentCount = a.Residents.Count(r => r.MoveOutDate == null),
                Balance = a.Invoices.Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                    .Sum(i => (decimal?)i.Balance) ?? 0
            })
            .ToListAsync();

        ViewBag.Projects = await Db.Projects.OrderBy(p => p.Name).ToListAsync();
        ViewBag.Buildings = await Db.Buildings.Where(b => !projectId.HasValue || b.ProjectId == projectId)
            .OrderBy(b => b.Code).ToListAsync();
        ViewBag.Q = q; ViewBag.Occupancy = occupancy; ViewBag.ProjectId = projectId;
        ViewBag.BuildingId = buildingId; ViewBag.Debt = debt;
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = p => Url.Action(nameof(Index), new { q, occupancy, projectId, buildingId, debt, page = p }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var rows = await Db.Apartments
            .Where(a => a.UnitCode.Contains(q) || a.Residents.Any(r => r.MoveOutDate == null && r.FullName.Contains(q)))
            .OrderBy(a => a.UnitCode)
            .Take(limit)
            .Select(a => new
            {
                title = a.UnitCode,
                subtitle = a.Building.Name + " · Tầng " + a.Floor.Number,
                url = Url.Action(nameof(Details), new { id = a.Id }) ?? "#",
                icon = "apartment"
            })
            .ToListAsync();
        return Json(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("apartments");
        var vm = await LoadDetailAsync(id, recentLimit: 24);
        if (vm == null) return NotFound();
        var apt = vm.Apartment;
        var crumbs = new List<(string, string?)> { ("Khu & toà nhà", Url.Action("Index", "Projects")) };
        if (apt.Building.Project != null)
            crumbs.Add((apt.Building.Project.Name, Url.Action("Details", "Projects", new { id = apt.Building.ProjectId })));
        crumbs.Add((apt.Building.Name, Url.Action("Details", "Buildings", new { id = apt.BuildingId })));
        crumbs.Add((apt.UnitCode, null));
        SetBreadcrumb(crumbs.ToArray());
        return View(vm);
    }

    /// <summary>Bản rút gọn để hiện trong ngăn trượt khi bấm vào ô căn trên sơ đồ toà.</summary>
    public async Task<IActionResult> Panel(int id)
    {
        var vm = await LoadDetailAsync(id, recentLimit: 4);
        if (vm == null) return NotFound();
        return PartialView("_ApartmentPanel", vm);
    }

    private async Task<ApartmentDetailVm?> LoadDetailAsync(int id, int recentLimit)
    {
        var apt = await Db.Apartments
            .Include(a => a.Building).ThenInclude(b => b.Project)
            .Include(a => a.Floor)
            .Include(a => a.Residents.OrderBy(r => r.MoveOutDate != null).ThenByDescending(r => r.IsPrimary).ThenBy(r => r.Role))
                .ThenInclude(r => r.User)
            .Include(a => a.RecurringCharges.OrderBy(c => c.SortOrder))
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);
        if (apt == null) return null;

        var invoices = Db.Invoices.Where(i => i.ApartmentId == id);
        return new ApartmentDetailVm
        {
            Apartment = apt,
            Invoices = await invoices.OrderByDescending(i => i.IssueDate).Take(recentLimit).ToListAsync(),
            Readings = await Db.UtilityReadings.Include(r => r.UtilityType).Where(r => r.ApartmentId == id)
                .OrderByDescending(r => r.BillingMonth).ThenBy(r => r.UtilityTypeId).Take(recentLimit * 2).ToListAsync(),
            Maintenance = await Db.MaintenanceRequests.Include(m => m.AssignedTo).Where(m => m.ApartmentId == id)
                .OrderByDescending(m => m.ReportedAt).Take(recentLimit).ToListAsync(),
            Balance = await invoices.Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                .SumAsync(i => (decimal?)i.Balance) ?? 0,
            OverdueBalance = await invoices.Where(i => i.Balance > 0 && i.DueDate < VnTime.Today
                    && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                .SumAsync(i => (decimal?)i.Balance) ?? 0,
            NewResident = new ResidentEditVm { ApartmentId = id, Role = apt.Residents.Any(r => r.IsActive) ? ResidentRole.Member : ResidentRole.Owner },
            NewCharge = new RecurringChargeEditVm { ApartmentId = id }
        };
    }

    public async Task<IActionResult> Create(int? buildingId, int? floorId)
    {
        SetActiveNav("apartments");
        SetBreadcrumb(("Căn hộ", Url.Action(nameof(Index))), ("Thêm căn", null));
        var vm = new ApartmentEditVm { BuildingId = buildingId ?? 0, FloorId = floorId ?? 0, Area = 70, Bedrooms = 2, Bathrooms = 2 };
        await PopulateLookupsAsync(vm);
        return View("Edit", vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        SetActiveNav("apartments");
        var apt = await Db.Apartments.FirstOrDefaultAsync(a => a.Id == id);
        if (apt == null) return NotFound();
        SetBreadcrumb(("Căn hộ", Url.Action(nameof(Index))), (apt.UnitCode, Url.Action(nameof(Details), new { id })), ("Sửa", null));

        var vm = new ApartmentEditVm
        {
            Id = apt.Id, UnitCode = apt.UnitCode, BuildingId = apt.BuildingId, FloorId = apt.FloorId,
            Area = apt.Area, Bedrooms = apt.Bedrooms, Bathrooms = apt.Bathrooms,
            HouseDirection = apt.HouseDirection, Occupancy = apt.Occupancy, Note = apt.Note
        };
        await PopulateLookupsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ApartmentEditVm input)
    {
        input.UnitCode = input.UnitCode?.Trim() ?? string.Empty;
        if (!await Db.Floors.AnyAsync(f => f.Id == input.FloorId && f.BuildingId == input.BuildingId))
            ModelState.AddModelError(nameof(input.FloorId), "Tầng không thuộc toà đã chọn.");
        if (await Db.Apartments.AnyAsync(a => a.BuildingId == input.BuildingId && a.UnitCode == input.UnitCode && a.Id != input.Id))
            ModelState.AddModelError(nameof(input.UnitCode), "Mã căn đã tồn tại trong toà này.");
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(input);
            return View(input);
        }

        Apartment apt;
        if (input.Id == 0)
        {
            apt = new Apartment();
            Db.Apartments.Add(apt);
        }
        else
        {
            apt = await Db.Apartments.FirstOrDefaultAsync(a => a.Id == input.Id) ?? throw new InvalidOperationException("Not found");
        }

        apt.UnitCode = input.UnitCode; apt.BuildingId = input.BuildingId; apt.FloorId = input.FloorId;
        apt.Area = input.Area; apt.Bedrooms = input.Bedrooms; apt.Bathrooms = input.Bathrooms;
        apt.HouseDirection = input.HouseDirection; apt.Occupancy = input.Occupancy; apt.Note = input.Note;

        await Db.SaveChangesAsync();
        TempData["Success"] = input.Id == 0 ? $"Đã thêm căn {apt.UnitCode}." : $"Đã cập nhật căn {apt.UnitCode}.";
        return RedirectToAction(nameof(Details), new { id = apt.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, ApartmentOccupancy status, string? returnUrl)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        var apt = await Db.Apartments.FindAsync(id);
        if (apt == null) return NotFound();
        apt.Occupancy = status;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Căn {apt.UnitCode} → {status.Vi()}.";
        return RedirectToLocalOrDefault(returnUrl, Url.Action(nameof(Details), new { id })!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateInvoice(int id)
    {
        var result = await _invoiceGen.GenerateMonthlyInvoiceAsync(id, VnTime.Today);
        TempData[result.Success ? "Success" : "Warning"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var apt = await Db.Apartments.FindAsync(id);
        if (apt == null) return NotFound();
        if (await Db.Invoices.AnyAsync(i => i.ApartmentId == id) || await Db.ApartmentResidents.AnyAsync(r => r.ApartmentId == id && r.MoveOutDate == null))
        {
            TempData["Danger"] = "Không xoá được căn đang có cư dân hoặc đã phát sinh hoá đơn.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var buildingId = apt.BuildingId;
        Db.Apartments.Remove(apt);
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã xoá căn {apt.UnitCode}.";
        return RedirectToAction("Details", "Buildings", new { id = buildingId });
    }

    // ───────────── Cư dân ─────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddResident(ResidentEditVm input)
    {
        var apt = await Db.Apartments.Include(a => a.Residents).FirstOrDefaultAsync(a => a.Id == input.ApartmentId);
        if (apt == null) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["Danger"] = string.Join(" · ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = input.ApartmentId });
        }

        var email = input.Email?.Trim();
        var user = string.IsNullOrEmpty(email) ? null : await _userManager.FindByEmailAsync(email);
        var active = apt.Residents.Where(r => r.IsActive).ToList();
        var resident = new ApartmentResident
        {
            ApartmentId = apt.Id,
            FullName = input.FullName.Trim(),
            Phone = input.Phone?.Trim(),
            Email = email,
            IdNumber = input.IdNumber?.Trim(),
            Role = input.Role,
            IsPrimary = input.IsPrimary || active.Count == 0,
            MoveInDate = input.MoveInDate,
            Note = input.Note,
            UserId = user?.Id
        };
        if (resident.IsPrimary) active.ForEach(r => r.IsPrimary = false);
        Db.ApartmentResidents.Add(resident);

        // Có người vào ở thì căn chuyển sang "Đang ở" (trừ khi đang sửa chữa).
        if (apt.Occupancy is ApartmentOccupancy.Vacant or ApartmentOccupancy.NotHandedOver)
            apt.Occupancy = ApartmentOccupancy.Occupied;

        // Cư dân có tài khoản được vào cổng cư dân.
        if (user != null && !await _userManager.IsInRoleAsync(user, SeedData.RoleTenant))
            await _userManager.AddToRoleAsync(user, SeedData.RoleTenant);

        await Db.SaveChangesAsync();

        // Chưa có tài khoản nhưng có email: gửi thư mời tạo tài khoản, đăng ký qua thư là tự gắn vào căn.
        var invited = user == null && email != null && await SendInviteAsync(resident, apt.UnitCode);
        TempData["Success"] = $"Đã thêm {resident.FullName} vào căn {apt.UnitCode}"
            + (user != null ? " (đã liên kết tài khoản cổng cư dân)."
               : invited ? $" và gửi thư mời tạo tài khoản tới {email}." : ".");
        return RedirectToAction(nameof(Details), new { id = apt.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendInvite(int residentId)
    {
        var r = await Db.ApartmentResidents.Include(x => x.Apartment)
            .FirstOrDefaultAsync(x => x.Id == residentId && x.MoveOutDate == null && x.UserId == null && x.Email != null);
        if (r == null) return NotFound();
        if (await SendInviteAsync(r, r.Apartment.UnitCode))
            TempData["Success"] = $"Đã gửi lại thư mời tới {r.Email}.";
        else
            TempData["Danger"] = $"Không gửi được thư tới {r.Email}. Kiểm tra cấu hình email rồi thử lại.";
        return RedirectToAction(nameof(Details), new { id = r.ApartmentId });
    }

    private Task<bool> SendInviteAsync(ApartmentResident r, string unitCode)
    {
        var url = Url.Action("Invite", "Home", new { area = "", token = _invites.CreateToken(r) }, Request.Scheme)!;
        return _invites.SendAsync(r, unitCode, url);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveOut(int residentId)
    {
        var r = await Db.ApartmentResidents.Include(x => x.Apartment).ThenInclude(a => a.Residents)
            .FirstOrDefaultAsync(x => x.Id == residentId && x.MoveOutDate == null);
        if (r == null) return NotFound();
        r.MoveOutDate = VnTime.Today;
        var stillLiving = r.Apartment.Residents.Where(x => x.Id != r.Id && x.IsActive).ToList();
        if (r.IsPrimary)
        {
            r.IsPrimary = false;
            var next = stillLiving.OrderBy(x => x.Role).FirstOrDefault();
            if (next != null) next.IsPrimary = true;
        }
        if (stillLiving.Count == 0 && r.Apartment.Occupancy == ApartmentOccupancy.Occupied)
            r.Apartment.Occupancy = ApartmentOccupancy.Vacant;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã ghi nhận {r.FullName} chuyển đi.";
        return RedirectToAction(nameof(Details), new { id = r.ApartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimary(int residentId)
    {
        var r = await Db.ApartmentResidents.Include(x => x.Apartment).ThenInclude(a => a.Residents)
            .FirstOrDefaultAsync(x => x.Id == residentId && x.MoveOutDate == null);
        if (r == null) return NotFound();
        foreach (var x in r.Apartment.Residents) x.IsPrimary = x.Id == r.Id;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"{r.FullName} là người đại diện căn.";
        return RedirectToAction(nameof(Details), new { id = r.ApartmentId });
    }

    // ───────────── Phí cố định hằng tháng ─────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCharge(RecurringChargeEditVm input)
    {
        if (!await Db.Apartments.AnyAsync(a => a.Id == input.ApartmentId)) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["Danger"] = "Khoản phí không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id = input.ApartmentId });
        }
        var nextSort = await Db.RecurringCharges.Where(c => c.ApartmentId == input.ApartmentId)
            .Select(c => (int?)c.SortOrder).MaxAsync() ?? -1;
        Db.RecurringCharges.Add(new RecurringCharge
        {
            ApartmentId = input.ApartmentId, Description = input.Description.Trim(),
            Amount = input.Amount, SortOrder = nextSort + 1, IsActive = true
        });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm khoản phí cố định.";
        return RedirectToAction(nameof(Details), new { id = input.ApartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCharge(int chargeId)
    {
        var c = await Db.RecurringCharges.FindAsync(chargeId);
        if (c == null) return NotFound();
        c.IsActive = !c.IsActive;
        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = c.ApartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCharge(int chargeId)
    {
        var c = await Db.RecurringCharges.FindAsync(chargeId);
        if (c == null) return NotFound();
        Db.RecurringCharges.Remove(c);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá khoản phí.";
        return RedirectToAction(nameof(Details), new { id = c.ApartmentId });
    }

    public async Task<IActionResult> FloorsOf(int buildingId) =>
        Json(await Db.Floors.Where(f => f.BuildingId == buildingId).OrderBy(f => f.Number)
            .Select(f => new { id = f.Id, label = f.Label }).ToListAsync());

    private async Task PopulateLookupsAsync(ApartmentEditVm vm)
    {
        vm.Buildings = await Db.Buildings.Include(b => b.Project).OrderBy(b => b.Code).ToListAsync();
        if (vm.BuildingId == 0 && vm.Buildings.Count > 0) vm.BuildingId = vm.Buildings[0].Id;
        vm.Floors = await Db.Floors.Where(f => f.BuildingId == vm.BuildingId).OrderBy(f => f.Number).ToListAsync();
    }
}
