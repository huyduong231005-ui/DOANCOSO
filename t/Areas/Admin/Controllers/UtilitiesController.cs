using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class UtilitiesController : AdminBaseController
{
    public UtilitiesController(AppDbContext db) : base(db) { }

    // ── Readings ──
    public async Task<IActionResult> Index(int? leaseId, int? utilityTypeId, int? billingMonth, int page = 1)
    {
        SetActiveNav("utilities");
        SetBreadcrumb(("Điện / Nước", null));

        const int pageSize = 30;
        var query = Db.UtilityReadings
            .Include(r => r.Lease).ThenInclude(l => l.PrimaryTenant)
            .Include(r => r.Lease).ThenInclude(l => l.Apartment)
            .Include(r => r.UtilityType)
            .AsQueryable();

        if (leaseId.HasValue) query = query.Where(r => r.LeaseId == leaseId.Value);
        if (utilityTypeId.HasValue) query = query.Where(r => r.UtilityTypeId == utilityTypeId.Value);
        if (billingMonth.HasValue) query = query.Where(r => r.BillingMonth == billingMonth.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => r.BillingMonth).ThenBy(r => r.UtilityType.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new UtilityReadingListVm
            {
                Id = r.Id, LeaseId = r.LeaseId,
                LeaseNumber = r.Lease.LeaseNumber,
                TenantName = r.Lease.PrimaryTenant.FullName,
                UnitTitle = r.Lease.Apartment.Title,
                UtilityName = r.UtilityType.Name,
                UtilityIcon = r.UtilityType.Icon ?? string.Empty,
                BillingMonth = r.BillingMonth,
                Previous = r.PreviousReading, Current = r.CurrentReading,
                Consumption = r.Consumption, Amount = r.Amount,
                Billed = r.Billed
            }).ToListAsync();

        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { leaseId, utilityTypeId, billingMonth, page = pp }) ?? "#"
        };
        ViewBag.LeaseId = leaseId; ViewBag.UtilityTypeId = utilityTypeId; ViewBag.BillingMonth = billingMonth;
        ViewBag.UtilityTypes = await Db.UtilityTypes.OrderBy(x => x.Name).ToListAsync();
        return View(rows);
    }

    public async Task<IActionResult> CreateReading(int? leaseId, int? utilityTypeId)
    {
        SetActiveNav("utilities");
        SetBreadcrumb(("Điện / Nước", Url.Action(nameof(Index))), ("Nhập chỉ số", null));

        var now = VnTime.Now;
        var vm = new UtilityReadingEditVm
        {
            BillingMonth = now.Year * 100 + now.Month,
            LeaseId = leaseId ?? 0,
            UtilityTypeId = utilityTypeId ?? 0
        };
        if (vm.LeaseId > 0 && vm.UtilityTypeId > 0)
            await PrefillFromPreviousAsync(vm);
        await PopulateReadingLookupsAsync(vm);
        return View("EditReading", vm);
    }

    public async Task<IActionResult> EditReading(int id)
    {
        SetActiveNav("utilities");
        var r = await Db.UtilityReadings.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Billed)
        {
            TempData["Danger"] = "Chỉ số đã được tính vào hoá đơn — không thể sửa. Liên hệ kế toán để điều chỉnh hoá đơn.";
            return RedirectToAction(nameof(Index));
        }
        SetBreadcrumb(("Điện / Nước", Url.Action(nameof(Index))), ($"#{id}", null));

        var vm = new UtilityReadingEditVm
        {
            Id = r.Id, LeaseId = r.LeaseId, UtilityTypeId = r.UtilityTypeId,
            BillingMonth = r.BillingMonth,
            PreviousReading = r.PreviousReading, CurrentReading = r.CurrentReading,
            Rate = r.Rate, Note = r.Note
        };
        await PopulateReadingLookupsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReading(UtilityReadingEditVm input)
    {
        if (!ModelState.IsValid)
        {
            await PopulateReadingLookupsAsync(input);
            return View(input);
        }

        var utilityType = await Db.UtilityTypes.FirstOrDefaultAsync(t => t.Id == input.UtilityTypeId);
        if (utilityType == null)
        {
            ModelState.AddModelError(nameof(input.UtilityTypeId), "Loại tiện ích không tồn tại.");
            await PopulateReadingLookupsAsync(input);
            return View(input);
        }

        // Metered mode requires CurrentReading >= PreviousReading. Fixed mode ignores readings.
        if (utilityType.BillingMode == UtilityBillingMode.Metered && input.CurrentReading < input.PreviousReading)
        {
            ModelState.AddModelError(nameof(input.CurrentReading), "Chỉ số mới phải >= chỉ số cũ.");
            await PopulateReadingLookupsAsync(input);
            return View(input);
        }

        // Pre-check unique (LeaseId, UtilityTypeId, BillingMonth) before letting the DB error.
        var duplicate = await Db.UtilityReadings.AnyAsync(x =>
            x.Id != input.Id &&
            x.LeaseId == input.LeaseId &&
            x.UtilityTypeId == input.UtilityTypeId &&
            x.BillingMonth == input.BillingMonth);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(input.BillingMonth), "Đã có chỉ số cho hợp đồng + loại + kỳ này. Hãy sửa bản ghi cũ thay vì tạo mới.");
            await PopulateReadingLookupsAsync(input);
            return View(input);
        }

        UtilityReading r;
        if (input.Id == 0)
        {
            r = new UtilityReading();
            Db.UtilityReadings.Add(r);
        }
        else
        {
            r = await Db.UtilityReadings.FirstOrDefaultAsync(x => x.Id == input.Id) ?? throw new InvalidOperationException();
            if (r.Billed)
            {
                TempData["Danger"] = "Chỉ số đã tính vào hoá đơn — không thể sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        r.LeaseId = input.LeaseId;
        r.UtilityTypeId = input.UtilityTypeId;
        r.BillingMonth = input.BillingMonth;
        r.Rate = input.Rate;
        r.Note = input.Note;
        r.ReadAt = DateTime.UtcNow;

        if (utilityType.BillingMode == UtilityBillingMode.Fixed)
        {
            // Fixed: amount is the rate itself; consumption/readings are not meaningful.
            r.PreviousReading = 0;
            r.CurrentReading = 0;
            r.Consumption = 0;
            r.Amount = Math.Round(input.Rate, 0);
        }
        else
        {
            r.PreviousReading = input.PreviousReading;
            r.CurrentReading = input.CurrentReading;
            r.Consumption = input.CurrentReading - input.PreviousReading;
            r.Amount = Math.Round(r.Consumption * input.Rate, 0);
        }

        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu chỉ số.";
        return RedirectToAction(nameof(Index), new { leaseId = input.LeaseId, billingMonth = input.BillingMonth });
    }

    /// <summary>Fill PreviousReading from the most recent reading on the same lease+type.</summary>
    private async Task PrefillFromPreviousAsync(UtilityReadingEditVm vm)
    {
        var prev = await Db.UtilityReadings
            .Where(r => r.LeaseId == vm.LeaseId && r.UtilityTypeId == vm.UtilityTypeId && r.BillingMonth < vm.BillingMonth)
            .OrderByDescending(r => r.BillingMonth)
            .Select(r => new { r.CurrentReading, r.Rate })
            .FirstOrDefaultAsync();
        if (prev != null)
        {
            vm.PreviousReading = prev.CurrentReading;
            if (vm.Rate == 0) vm.Rate = prev.Rate;
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReading(int id)
    {
        var r = await Db.UtilityReadings.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Billed) { TempData["Danger"] = "Đã tính vào hoá đơn — không thể xoá."; return RedirectToAction(nameof(Index)); }
        Db.UtilityReadings.Remove(r);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá chỉ số.";
        return RedirectToAction(nameof(Index));
    }

    // ── Types ──
    public async Task<IActionResult> Types()
    {
        SetActiveNav("utilities");
        SetBreadcrumb(("Điện / Nước", Url.Action(nameof(Index))), ("Loại tiện ích", null));
        var rows = await Db.UtilityTypes.OrderBy(x => x.Name).ToListAsync();
        return View(rows);
    }

    public IActionResult CreateType()
    {
        SetActiveNav("utilities");
        SetBreadcrumb(("Điện / Nước", Url.Action(nameof(Index))), ("Loại tiện ích", Url.Action(nameof(Types))), ("Thêm", null));
        return View("EditType", new UtilityTypeEditVm());
    }

    public async Task<IActionResult> EditType(int id)
    {
        SetActiveNav("utilities");
        var t = await Db.UtilityTypes.FindAsync(id);
        if (t == null) return NotFound();
        SetBreadcrumb(("Điện / Nước", Url.Action(nameof(Index))), ("Loại tiện ích", Url.Action(nameof(Types))), (t.Name, null));
        var vm = new UtilityTypeEditVm
        {
            Id = t.Id, Code = t.Code, Name = t.Name, Unit = t.Unit,
            BillingMode = t.BillingMode, DefaultRate = t.DefaultRate, Icon = t.Icon
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditType(UtilityTypeEditVm input)
    {
        if (!ModelState.IsValid) return View(input);
        UtilityType t;
        if (input.Id == 0)
        {
            t = new UtilityType();
            Db.UtilityTypes.Add(t);
        }
        else
        {
            t = await Db.UtilityTypes.FindAsync(input.Id) ?? throw new InvalidOperationException();
        }
        t.Code = input.Code; t.Name = input.Name; t.Unit = input.Unit;
        t.BillingMode = input.BillingMode; t.DefaultRate = input.DefaultRate;
        t.Icon = input.Icon;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu loại tiện ích.";
        return RedirectToAction(nameof(Types));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteType(int id)
    {
        var t = await Db.UtilityTypes.Include(x => x.Readings).FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        if (t.Readings.Any())
        {
            TempData["Danger"] = "Không thể xoá vì còn chỉ số tham chiếu.";
            return RedirectToAction(nameof(Types));
        }
        Db.UtilityTypes.Remove(t);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá.";
        return RedirectToAction(nameof(Types));
    }

    private async Task PopulateReadingLookupsAsync(UtilityReadingEditVm vm)
    {
        vm.Leases = await Db.Leases
            .Include(l => l.Apartment).Include(l => l.PrimaryTenant)
            .Where(l => l.Status == LeaseStatus.Active)
            .OrderBy(l => l.LeaseNumber).ToListAsync();
        vm.UtilityTypes = await Db.UtilityTypes.OrderBy(x => x.Name).ToListAsync();
    }
}
