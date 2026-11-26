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
    public async Task<IActionResult> Index(int? apartmentId, int? buildingId, int? utilityTypeId, int? billingMonth, int page = 1)
    {
        SetActiveNav("utilities");
        SetBreadcrumb(("Điện / Nước", null));

        const int pageSize = 30;
        var query = Db.UtilityReadings
            .Include(r => r.UtilityType)
            .AsQueryable();

        if (apartmentId.HasValue) query = query.Where(r => r.ApartmentId == apartmentId.Value);
        if (buildingId.HasValue) query = query.Where(r => r.Apartment.BuildingId == buildingId.Value);
        if (utilityTypeId.HasValue) query = query.Where(r => r.UtilityTypeId == utilityTypeId.Value);
        if (billingMonth.HasValue) query = query.Where(r => r.BillingMonth == billingMonth.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => r.BillingMonth).ThenBy(r => r.Apartment.UnitCode).ThenBy(r => r.UtilityType.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new UtilityReadingListVm
            {
                Id = r.Id, ApartmentId = r.ApartmentId,
                UnitCode = r.Apartment.UnitCode,
                BuildingName = "Toà " + r.Apartment.Building.Code,
                UtilityName = r.UtilityType.Name,
                BillingMonth = r.BillingMonth,
                Previous = r.PreviousReading, Current = r.CurrentReading,
                Consumption = r.CurrentReading - r.PreviousReading,
                // Tạm tính theo đơn giá hiện hành; số tiền chốt nằm ở dòng hoá đơn.
                Amount = Math.Round((r.CurrentReading - r.PreviousReading) * r.UtilityType.DefaultRate, 0),
                Billed = r.Apartment.Invoices.Any(i => i.BillingMonth == r.BillingMonth
                                                      && i.Kind == InvoiceKind.Monthly && i.Status != InvoiceStatus.Cancelled)
            }).ToListAsync();

        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { apartmentId, buildingId, utilityTypeId, billingMonth, page = pp }) ?? "#"
        };
        ViewBag.ApartmentId = apartmentId; ViewBag.BuildingId = buildingId;
        ViewBag.UtilityTypeId = utilityTypeId; ViewBag.BillingMonth = billingMonth;
        ViewBag.Buildings = await Db.Buildings.OrderBy(b => b.Code).ToListAsync();
        ViewBag.UtilityTypes = await MeterTypes().ToListAsync();
        return View(rows);
    }

    public async Task<IActionResult> CreateReading(int? apartmentId, int? utilityTypeId)
    {
        SetActiveNav("utilities");
        SetBreadcrumb(("Điện / Nước", Url.Action(nameof(Index))), ("Nhập chỉ số", null));

        var now = VnTime.Now;
        var vm = new UtilityReadingEditVm
        {
            BillingMonth = now.Year * 100 + now.Month,
            ApartmentId = apartmentId ?? 0,
            UtilityTypeId = utilityTypeId ?? 0
        };
        if (vm.ApartmentId > 0 && vm.UtilityTypeId > 0)
            await PrefillFromPreviousAsync(vm);
        await PopulateReadingLookupsAsync(vm);
        return View("EditReading", vm);
    }

    public async Task<IActionResult> EditReading(int id)
    {
        SetActiveNav("utilities");
        var r = await Db.UtilityReadings.Include(x => x.Apartment.Invoices).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (r.Billed)
        {
            TempData["Danger"] = "Chỉ số đã được tính vào hoá đơn — không thể sửa. Liên hệ kế toán để điều chỉnh hoá đơn.";
            return RedirectToAction(nameof(Index));
        }
        SetBreadcrumb(("Điện / Nước", Url.Action(nameof(Index))), ($"#{id}", null));

        var vm = new UtilityReadingEditVm
        {
            Id = r.Id, ApartmentId = r.ApartmentId, UtilityTypeId = r.UtilityTypeId,
            BillingMonth = r.BillingMonth,
            PreviousReading = r.PreviousReading, CurrentReading = r.CurrentReading
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

        if (!await Db.Apartments.AnyAsync(a => a.Id == input.ApartmentId))
            ModelState.AddModelError(nameof(input.ApartmentId), "Căn hộ không tồn tại.");
        else if (await BillingPeriodErrorAsync(input.ApartmentId, input.BillingMonth) is { } periodError)
            ModelState.AddModelError(nameof(input.BillingMonth), periodError);
        if (!ModelState.IsValid)
        {
            await PopulateReadingLookupsAsync(input);
            return View(input);
        }

        var utilityType = await MeterTypes().FirstOrDefaultAsync(t => t.Id == input.UtilityTypeId);
        if (utilityType == null)
        {
            ModelState.AddModelError(nameof(input.UtilityTypeId), "Loại tiện ích không tồn tại.");
            await PopulateReadingLookupsAsync(input);
            return View(input);
        }

        if (input.CurrentReading < input.PreviousReading)
        {
            ModelState.AddModelError(nameof(input.CurrentReading), "Chỉ số mới phải >= chỉ số cũ.");
            await PopulateReadingLookupsAsync(input);
            return View(input);
        }

        // Pre-check unique (ApartmentId, UtilityTypeId, BillingMonth) before letting the DB error.
        var duplicate = await Db.UtilityReadings.AnyAsync(x =>
            x.Id != input.Id &&
            x.ApartmentId == input.ApartmentId &&
            x.UtilityTypeId == input.UtilityTypeId &&
            x.BillingMonth == input.BillingMonth);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(input.BillingMonth), "Đã có chỉ số cho căn + loại + kỳ này. Hãy sửa bản ghi cũ thay vì tạo mới.");
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
            if ((r = (await Db.UtilityReadings.Include(x => x.Apartment.Invoices).FirstOrDefaultAsync(x => x.Id == input.Id))!) is null) return NotFound(); // Id gửi lên không tồn tại
            if (r.Billed)
            {
                TempData["Danger"] = "Chỉ số đã tính vào hoá đơn — không thể sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        r.ApartmentId = input.ApartmentId;
        r.UtilityTypeId = input.UtilityTypeId;
        r.BillingMonth = input.BillingMonth;
        r.ReadAt = DateTime.UtcNow;
        r.PreviousReading = input.PreviousReading;
        r.CurrentReading = input.CurrentReading;

        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu chỉ số.";
        return RedirectToAction(nameof(Index), new { apartmentId = input.ApartmentId, billingMonth = input.BillingMonth });
    }

    /// <summary>Fill PreviousReading from the most recent reading on the same apartment+type.</summary>
    private async Task PrefillFromPreviousAsync(UtilityReadingEditVm vm)
    {
        var prev = await Db.UtilityReadings
            .Where(r => r.ApartmentId == vm.ApartmentId && r.UtilityTypeId == vm.UtilityTypeId && r.BillingMonth < vm.BillingMonth)
            .OrderByDescending(r => r.BillingMonth)
            .Select(r => (decimal?)r.CurrentReading)
            .FirstOrDefaultAsync();
        if (prev != null) vm.PreviousReading = prev.Value;
    }

    // ── Ghi chỉ số hàng loạt: một toà, một loại đồng hồ, một kỳ ──
    public async Task<IActionResult> Bulk(int? buildingId, int? utilityTypeId, int? billingMonth)
    {
        SetActiveNav("utilities");
        SetBreadcrumb(("Điện / Nước", Url.Action(nameof(Index))), ("Ghi chỉ số theo toà", null));

        var now = VnTime.Now;
        var vm = new BulkReadingVm
        {
            BuildingId = buildingId ?? await Db.Buildings.OrderBy(b => b.Code).Select(b => b.Id).FirstOrDefaultAsync(),
            UtilityTypeId = utilityTypeId ?? await MeterTypes()
                .OrderBy(t => t.Id).Select(t => t.Id).FirstOrDefaultAsync(),
            BillingMonth = billingMonth ?? now.Year * 100 + now.Month,
            Buildings = await Db.Buildings.OrderBy(b => b.Code).ToListAsync(),
            UtilityTypes = await MeterTypes().ToListAsync()
        };
        var type = vm.UtilityTypes.FirstOrDefault(t => t.Id == vm.UtilityTypeId);
        vm.Rate = type?.DefaultRate ?? 0;

        var apartments = await Db.Apartments
            .Where(a => a.BuildingId == vm.BuildingId && a.Condition != ApartmentCondition.NotHandedOver)
            .OrderBy(a => a.FloorNumber).ThenBy(a => a.UnitCode)
            .Select(a => new { a.Id, a.UnitCode, FloorNumber = a.FloorNumber })
            .ToListAsync();
        var ids = apartments.Select(a => a.Id).ToList();
        var readings = await Db.UtilityReadings
            .Where(r => ids.Contains(r.ApartmentId) && r.UtilityTypeId == vm.UtilityTypeId && r.BillingMonth <= vm.BillingMonth)
            .Select(r => new { r.ApartmentId, r.BillingMonth, r.CurrentReading, Billed = r.Apartment.Invoices.Any(i => i.BillingMonth == r.BillingMonth
                                                                                     && i.Kind == InvoiceKind.Monthly && i.Status != InvoiceStatus.Cancelled) })
            .ToListAsync();

        vm.Rows = apartments.Select(a =>
        {
            var current = readings.FirstOrDefault(r => r.ApartmentId == a.Id && r.BillingMonth == vm.BillingMonth);
            var prev = readings.Where(r => r.ApartmentId == a.Id && r.BillingMonth < vm.BillingMonth)
                .OrderByDescending(r => r.BillingMonth).FirstOrDefault();
            return new BulkReadingRow
            {
                ApartmentId = a.Id, UnitCode = a.UnitCode, FloorNumber = a.FloorNumber,
                PreviousReading = prev?.CurrentReading ?? 0,
                CurrentReading = current?.CurrentReading,
                Locked = current?.Billed == true
            };
        }).ToList();
        return View(vm);
    }

    private static bool IsValidBillingMonth(int yyyymm) =>
        yyyymm is >= 202000 and <= 209912 && yyyymm % 100 is >= 1 and <= 12;

    private Task<List<int>> InvoicedApartmentIdsAsync(List<int> apartmentIds, int billingMonth) =>
        Db.Invoices.Where(i => apartmentIds.Contains(i.ApartmentId) && i.BillingMonth == billingMonth
                               && i.Kind == InvoiceKind.Monthly && i.Status != InvoiceStatus.Cancelled)
            .Select(i => i.ApartmentId).Distinct().ToListAsync();

    /// <summary>Lý do không nhận chỉ số cho kỳ này, hoặc null nếu nhận được.</summary>
    private async Task<string?> BillingPeriodErrorAsync(int apartmentId, int billingMonth)
    {
        if (!IsValidBillingMonth(billingMonth)) return "Kỳ không hợp lệ (dạng yyyyMM, tháng từ 01 đến 12).";
        if ((await InvoicedApartmentIdsAsync(new() { apartmentId }, billingMonth)).Count > 0)
            return "Căn đã lập hoá đơn tháng cho kỳ này nên chỉ số sẽ không được tính tiền. Ghi vào kỳ sau, hoặc huỷ hoá đơn kỳ này rồi lập lại.";
        return null;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(BulkReadingVm input)
    {
        var type = await MeterTypes().FirstOrDefaultAsync(t => t.Id == input.UtilityTypeId);
        if (type == null || !IsValidBillingMonth(input.BillingMonth)) return BadRequest();

        var ids = input.Rows.Select(r => r.ApartmentId).ToList();
        var validIds = await Db.Apartments.Where(a => ids.Contains(a.Id) && a.BuildingId == input.BuildingId)
            .Select(a => a.Id).ToListAsync();
        // Căn đã lập hoá đơn tháng kỳ này: chỉ số ghi thêm sẽ không bao giờ được tính tiền, nên bỏ qua.
        var invoiced = await InvoicedApartmentIdsAsync(validIds, input.BillingMonth);
        var skippedInvoiced = input.Rows.Count(r => r.CurrentReading.HasValue && invoiced.Contains(r.ApartmentId));
        validIds = validIds.Except(invoiced).ToList();
        var existing = await Db.UtilityReadings
            .Include(r => r.Apartment.Invoices)
            .Where(r => validIds.Contains(r.ApartmentId) && r.UtilityTypeId == type.Id && r.BillingMonth == input.BillingMonth)
            .ToListAsync();

        int saved = 0, skipped = 0;
        foreach (var row in input.Rows.Where(r => r.CurrentReading.HasValue && validIds.Contains(r.ApartmentId)).DistinctBy(r => r.ApartmentId))
        {
            if (row.CurrentReading < row.PreviousReading) { skipped++; continue; }
            var r = existing.FirstOrDefault(x => x.ApartmentId == row.ApartmentId);
            if (r?.Billed == true) { skipped++; continue; }
            if (r == null)
            {
                r = new UtilityReading { ApartmentId = row.ApartmentId, UtilityTypeId = type.Id, BillingMonth = input.BillingMonth };
                Db.UtilityReadings.Add(r);
            }
            r.PreviousReading = row.PreviousReading;
            r.CurrentReading = row.CurrentReading!.Value;
            r.ReadAt = DateTime.UtcNow;
            saved++;
        }
        await Db.SaveChangesAsync();
        skipped += skippedInvoiced;
        TempData[skipped > 0 ? "Warning" : "Success"] = $"Đã lưu {saved} chỉ số {type.Name}."
            + (skipped > 0 ? $" Bỏ qua {skipped} căn (chỉ số mới nhỏ hơn chỉ số cũ, hoặc kỳ này đã lập hoá đơn)." : "");
        return RedirectToAction(nameof(Bulk), new { input.BuildingId, input.UtilityTypeId, input.BillingMonth });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReading(int id)
    {
        var r = await Db.UtilityReadings.Include(x => x.Apartment.Invoices).FirstOrDefaultAsync(x => x.Id == id);
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
            DefaultRate = t.DefaultRate, Method = t.Method
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
            if ((t = (await Db.UtilityTypes.FindAsync(input.Id))!) is null) return NotFound(); // Id gửi lên không tồn tại
        }
        t.Code = input.Code; t.Name = input.Name; t.Unit = input.Unit;
        t.DefaultRate = input.DefaultRate;
        t.Method = input.Method;
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
        vm.Apartments = await Db.Apartments
            .Where(a => a.Condition != ApartmentCondition.NotHandedOver || a.Id == vm.ApartmentId)
            .OrderBy(a => a.Building.Code).ThenBy(a => a.UnitCode)
            .Select(a => new ApartmentOption { Id = a.Id, Label = a.UnitCode + " · " + "Toà " + a.Building.Code })
            .ToListAsync();
        vm.UtilityTypes = await MeterTypes().ToListAsync();
    }

    /// <summary>Chỉ dịch vụ tính theo chỉ số đồng hồ mới có chỉ số.</summary>
    private IQueryable<UtilityType> MeterTypes() =>
        Db.UtilityTypes.Where(t => t.Method == BillingMethod.ByMeter).OrderBy(t => t.Name);
}
