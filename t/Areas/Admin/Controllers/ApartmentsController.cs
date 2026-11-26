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

[t.Infrastructure.Security.Permission("apartments.view", "apartments.update")]
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
                (r.User.FullName.Contains(q) || (r.User.PhoneNumber != null && r.User.PhoneNumber.Contains(q)))));
        query = occupancy switch
        {
            ApartmentOccupancy.Occupied => query.Where(Apartment.IsOccupied),
            ApartmentOccupancy.Vacant => query.Where(Apartment.IsVacant),
            ApartmentOccupancy o => query.Where(a => (int)a.Condition == (int)o),
            null => query
        };
        if (projectId.HasValue) query = query.Where(a => a.Building.ProjectId == projectId.Value);
        if (buildingId.HasValue) query = query.Where(a => a.BuildingId == buildingId.Value);
        if (debt) query = query.Where(a => a.Invoices.Any(i => i.Balance > 0 && i.DueDate < today
            && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft));

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(a => a.Building.Code).ThenBy(a => a.FloorNumber).ThenBy(a => a.UnitCode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ApartmentListVm
            {
                Id = a.Id,
                UnitCode = a.UnitCode,
                BuildingName = "Toà " + a.Building.Code,
                ProjectName = a.Building.Project != null ? a.Building.Project.Name : "",
                FloorNumber = a.FloorNumber,
                Area = a.Area,
                Bedrooms = a.Bedrooms,
                Condition = a.Condition,
                HasResident = a.Residents.Any(r => r.MoveOutDate == null),
                RepresentativeName = a.Residents.Where(r => r.MoveOutDate == null)
                    .OrderByDescending(r => r.IsPrimary).ThenBy(r => r.Role).Select(r => r.User.FullName).FirstOrDefault(),
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
            .Where(a => a.UnitCode.Contains(q) || a.Residents.Any(r => r.MoveOutDate == null && r.User.FullName.Contains(q)))
            .OrderBy(a => a.UnitCode)
            .Take(limit)
            .Select(a => new
            {
                title = a.UnitCode,
                subtitle = "Toà " + a.Building.Code + " · Tầng " + a.FloorNumber,
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
            .Include(a => a.Residents.OrderBy(r => r.MoveOutDate != null).ThenByDescending(r => r.IsPrimary).ThenBy(r => r.Role))
                .ThenInclude(r => r.User)
            .Include(a => a.RecurringCharges.OrderBy(c => c.UtilityTypeId))
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id);
        if (apt == null) return null;

        var invoices = Db.Invoices.Where(i => i.ApartmentId == id);
        return new ApartmentDetailVm
        {
            Apartment = apt,
            Invoices = await invoices.OrderByDescending(i => i.IssueDate).Take(recentLimit).ToListAsync(),
            Readings = await Db.UtilityReadings.Include(r => r.UtilityType).Include(r => r.Apartment.Invoices).Where(r => r.ApartmentId == id)
                .OrderByDescending(r => r.BillingMonth).ThenBy(r => r.UtilityTypeId).Take(recentLimit * 2).ToListAsync(),
            Maintenance = await Db.MaintenanceRequests.Include(m => m.AssignedTo).Where(m => m.ApartmentId == id)
                .OrderByDescending(m => m.ReportedAt).Take(recentLimit).ToListAsync(),
            Balance = await invoices.Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                .SumAsync(i => (decimal?)i.Balance) ?? 0,
            OverdueBalance = await invoices.Where(i => i.Balance > 0 && i.DueDate < VnTime.Today
                    && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft)
                .SumAsync(i => (decimal?)i.Balance) ?? 0,
            NewResident = new ResidentEditVm { ApartmentId = id, Role = apt.Residents.Any(r => r.IsActive) ? ResidentRole.Member : ResidentRole.Owner },
            NewCharge = new RecurringChargeEditVm { ApartmentId = id },
            ManagementType = await Db.UtilityTypes.FirstOrDefaultAsync(t => t.Method == BillingMethod.ByArea),
            SubscriptionTypes = await Db.UtilityTypes.Where(t => t.Method == BillingMethod.BySubscription).OrderBy(t => t.Name).ToListAsync()
        };
    }

    public async Task<IActionResult> Create(int? buildingId, int? floor)
    {
        SetActiveNav("apartments");
        SetBreadcrumb(("Căn hộ", Url.Action(nameof(Index))), ("Thêm căn", null));
        var vm = new ApartmentEditVm { BuildingId = buildingId ?? 0, FloorNumber = floor ?? 1, Area = 70, Bedrooms = 2 };
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
            Id = apt.Id, UnitCode = apt.UnitCode, BuildingId = apt.BuildingId, FloorNumber = apt.FloorNumber,
            Area = apt.Area, Bedrooms = apt.Bedrooms, Condition = apt.Condition
        };
        await PopulateLookupsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ApartmentEditVm input)
    {
        input.UnitCode = input.UnitCode?.Trim() ?? string.Empty;
        if (!await Db.Buildings.AnyAsync(b => b.Id == input.BuildingId))
            ModelState.AddModelError(nameof(input.BuildingId), "Toà không tồn tại.");
        if (await Db.Apartments.AnyAsync(a => a.BuildingId == input.BuildingId && a.UnitCode == input.UnitCode && a.Id != input.Id))
            ModelState.AddModelError(nameof(input.UnitCode), "Mã căn đã tồn tại trong toà này.");
        if (input.Id != 0 && await ConditionConflictAsync(input.Id, input.Condition) is { } conflict)
            ModelState.AddModelError(nameof(input.Condition), conflict);
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
            if ((apt = (await Db.Apartments.FirstOrDefaultAsync(a => a.Id == input.Id))!) is null) return NotFound(); // Id gửi lên không tồn tại
        }

        apt.UnitCode = input.UnitCode; apt.BuildingId = input.BuildingId; apt.FloorNumber = input.FloorNumber;
        apt.Area = input.Area; apt.Bedrooms = input.Bedrooms; apt.Condition = input.Condition;

        await Db.SaveChangesAsync();
        TempData["Success"] = input.Id == 0 ? $"Đã thêm căn {apt.UnitCode}." : $"Đã cập nhật căn {apt.UnitCode}.";
        return RedirectToAction(nameof(Details), new { id = apt.Id });
    }

    /// <summary>Căn còn cư dân đang ở thì không thể là Chưa bàn giao.</summary>
    private async Task<string?> ConditionConflictAsync(int apartmentId, ApartmentCondition status)
    {
        if (status != ApartmentCondition.NotHandedOver) return null;
        var living = await Db.ApartmentResidents.CountAsync(r => r.ApartmentId == apartmentId && r.MoveOutDate == null);
        return living == 0 ? null
            : $"Căn còn {living} cư dân đang ở. Ghi nhận chuyển đi trước khi đổi sang {status.Vi()}.";
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, ApartmentCondition status, string? returnUrl)
    {
        // Giá trị enum không hợp lệ bị model binding đổi thành 0 kèm lỗi, nên phải xem ModelState.
        if (!ModelState.IsValid || !Enum.IsDefined(status)) return BadRequest();
        var apt = await Db.Apartments.FindAsync(id);
        if (apt == null) return NotFound();
        if (await ConditionConflictAsync(id, status) is { } conflict)
        {
            TempData["Danger"] = conflict;
            return RedirectToLocalOrDefault(returnUrl, Url.Action(nameof(Details), new { id })!);
        }
        apt.Condition = status;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Căn {apt.UnitCode} → {status.Vi()}.";
        return RedirectToLocalOrDefault(returnUrl, Url.Action(nameof(Details), new { id })!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [t.Infrastructure.Security.Permission("invoices.issue")]
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
    [t.Infrastructure.Security.Permission("residents.manage")]
    public async Task<IActionResult> AddResident(ResidentEditVm input)
    {
        var apt = await Db.Apartments.Include(a => a.Residents).FirstOrDefaultAsync(a => a.Id == input.ApartmentId);
        if (apt == null) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["Danger"] = string.Join(" · ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = input.ApartmentId });
        }

        var email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        var active = apt.Residents.Where(r => r.IsActive).ToList();
        var phone = input.Phone?.Trim();
        var idNumber = string.IsNullOrWhiteSpace(input.IdNumber) ? null : input.IdNumber.Trim();
        var duplicate = active.FirstOrDefault(r =>
            (email != null && string.Equals(r.User.Email, email, StringComparison.OrdinalIgnoreCase))
            || (idNumber != null && r.User.IdNumber == idNumber)
            || (string.Equals(r.User.FullName.Trim(), input.FullName.Trim(), StringComparison.OrdinalIgnoreCase) && r.User.PhoneNumber == phone));
        if (duplicate != null)
        {
            TempData["Danger"] = $"{duplicate.User.FullName} đã có trong danh sách cư dân đang ở của căn {apt.UnitCode}.";
            return RedirectToAction(nameof(Details), new { id = apt.Id });
        }

        // Người và tài khoản là một (NguoiDung): email đã có tài khoản thì dùng lại tài khoản đó, chưa có thì tạo
        // tài khoản chưa đặt mật khẩu. Cư trú chỉ có hiệu lực trên cổng cư dân khi chủ hộp thư bấm thư mời xác nhận
        // (ConfirmedAt), nên BQL gõ nhầm email của người khác cũng không làm lộ căn (BR-CD-02).
        var person = email == null ? null
            : await _userManager.FindByEmailAsync(email);
        person ??= PendingAccounts.New(input.FullName, phone, email, idNumber);
        if (person.IdNumber == null && idNumber != null) person.IdNumber = idNumber;

        var resident = new ApartmentResident
        {
            ApartmentId = apt.Id,
            User = person,
            Role = input.Role,
            IsPrimary = input.IsPrimary || active.Count == 0,
            MoveInDate = input.MoveInDate
        };
        if (resident.IsPrimary) active.ForEach(r => r.IsPrimary = false);
        Db.ApartmentResidents.Add(resident);

        // Có người vào ở thì căn chắc chắn đã bàn giao; "Đang ở" tự suy ra từ CuTru.
        if (apt.Condition == ApartmentCondition.NotHandedOver)
            apt.Condition = ApartmentCondition.HandedOver;

        await Db.SaveChangesAsync();

        // Có email thì gửi thư mời xác nhận: tài khoản mới đặt mật khẩu, tài khoản đã có bấm để nhận căn.
        var invited = email != null && await SendInviteAsync(resident, apt.UnitCode);
        TempData["Success"] = $"Đã thêm {person.FullName} vào căn {apt.UnitCode}"
            + (invited ? $" và gửi thư mời tới {email}." : ".");
        return RedirectToAction(nameof(Details), new { id = apt.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [t.Infrastructure.Security.Permission("residents.manage")]
    public async Task<IActionResult> ResendInvite(int residentId)
    {
        var r = await Db.ApartmentResidents.Include(x => x.Apartment)
            .FirstOrDefaultAsync(x => x.Id == residentId && x.MoveOutDate == null && x.ConfirmedAt == null && x.User.Email != null);
        if (r == null) return NotFound();
        if (await SendInviteAsync(r, r.Apartment.UnitCode))
            TempData["Success"] = $"Đã gửi lại thư mời tới {r.User.Email}.";
        else
            TempData["Danger"] = $"Không gửi được thư tới {r.User.Email}. Kiểm tra cấu hình email rồi thử lại.";
        return RedirectToAction(nameof(Details), new { id = r.ApartmentId });
    }

    private Task<bool> SendInviteAsync(ApartmentResident r, string unitCode)
    {
        var url = Url.Action("Invite", "Home", new { area = "", token = _invites.CreateToken(r) }, Request.Scheme)!;
        return _invites.SendAsync(r, unitCode, url);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [t.Infrastructure.Security.Permission("residents.manage")]
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
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã ghi nhận {r.User.FullName} chuyển đi.";
        return RedirectToAction(nameof(Details), new { id = r.ApartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [t.Infrastructure.Security.Permission("residents.manage")]
    public async Task<IActionResult> SetPrimary(int residentId)
    {
        var r = await Db.ApartmentResidents.Include(x => x.Apartment).ThenInclude(a => a.Residents)
            .FirstOrDefaultAsync(x => x.Id == residentId && x.MoveOutDate == null);
        if (r == null) return NotFound();
        foreach (var x in r.Apartment.Residents) x.IsPrimary = x.Id == r.Id;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"{r.User.FullName} là người đại diện căn.";
        return RedirectToAction(nameof(Details), new { id = r.ApartmentId });
    }

    // ───────────── Dịch vụ căn đăng ký (gửi xe…) ─────────────

    /// <summary>Đăng ký hoặc đổi số lượng một dịch vụ cố định cho căn; tên và đơn giá lấy từ bảng giá.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCharge(RecurringChargeEditVm input)
    {
        if (!await Db.Apartments.AnyAsync(a => a.Id == input.ApartmentId)) return NotFound();
        var type = await Db.UtilityTypes.FirstOrDefaultAsync(t => t.Id == input.UtilityTypeId && t.Method == BillingMethod.BySubscription);
        if (!ModelState.IsValid || type == null)
        {
            TempData["Danger"] = "Đăng ký dịch vụ không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id = input.ApartmentId });
        }
        var c = await Db.RecurringCharges.FindAsync(input.ApartmentId, input.UtilityTypeId);
        if (c == null) Db.RecurringCharges.Add(new RecurringCharge { ApartmentId = input.ApartmentId, UtilityTypeId = type.Id, Quantity = input.Quantity });
        else c.Quantity = input.Quantity;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã đăng ký {type.Name} × {input.Quantity}.";
        return RedirectToAction(nameof(Details), new { id = input.ApartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCharge(int apartmentId, int utilityTypeId)
    {
        var c = await Db.RecurringCharges.FindAsync(apartmentId, utilityTypeId);
        if (c == null) return NotFound();
        Db.RecurringCharges.Remove(c);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã huỷ đăng ký dịch vụ.";
        return RedirectToAction(nameof(Details), new { id = apartmentId });
    }

    private async Task PopulateLookupsAsync(ApartmentEditVm vm)
    {
        vm.Buildings = await Db.Buildings.Include(b => b.Project).OrderBy(b => b.Code).ToListAsync();
        if (vm.BuildingId == 0 && vm.Buildings.Count > 0) vm.BuildingId = vm.Buildings[0].Id;
    }
}
