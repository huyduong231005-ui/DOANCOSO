using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Controllers;

[Authorize]
public class MyListingsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public MyListingsController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Challenge();

        var query = _db.Apartments
            .AsNoTracking()
            .Include(a => a.Images)
            .Include(a => a.Category)
            .Where(a => a.HostId == userId)
            .OrderByDescending(a => a.CreatedAt);

        var items = await query.Select(a => new MyListingItemViewModel
        {
            Id = a.Id,
            Title = a.Title,
            Price = a.Price,
            Area = a.Area,
            Bedrooms = a.Bedrooms,
            Bathrooms = a.Bathrooms,
            CategoryName = a.Category.Name,
            Address = a.Address,
            Status = a.Status,
            Occupancy = a.Occupancy,
            ViewCount = a.ViewCount,
            CreatedAt = a.CreatedAt,
            ModerationNote = a.ModerationNote,
            CoverImageUrl = a.Images.Where(i => i.IsCover).Select(i => i.Url).FirstOrDefault()
                            ?? a.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()
        }).ToListAsync();

        var vm = new MyListingsPageViewModel
        {
            Items = items,
            Total = items.Count,
            Active = items.Count(i => i.Status == ListingStatus.Active),
            Hidden = items.Count(i => i.Status == ListingStatus.Hidden)
        };

        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Challenge();

        var apartment = await _db.Apartments
            .AsNoTracking()
            .Include(a => a.Images)
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == id && a.HostId == userId);

        if (apartment is null) return NotFound();

        var vm = new EditListingViewModel
        {
            Id = apartment.Id,
            Title = apartment.Title,
            Description = apartment.Description,
            Price = apartment.Price,
            DefaultDeposit = apartment.DefaultDeposit,
            FeeNote = apartment.FeeNote,
            Address = apartment.Address,
            Area = apartment.Area,
            Bedrooms = apartment.Bedrooms,
            Bathrooms = apartment.Bathrooms,
            FurnishingLevel = apartment.FurnishingLevel,
            AllowsPets = apartment.AllowsPets,
            ParkingType = apartment.ParkingType,
            AvailableFrom = apartment.AvailableFrom,
            MinLeaseMonths = apartment.MinLeaseMonths,
            MaxLeaseMonths = apartment.MaxLeaseMonths,
            HouseDirection = apartment.HouseDirection,
            FloorNumber = apartment.FloorNumber,
            Status = apartment.Status,
            CategoryName = apartment.Category.Name,
            CoverImageUrl = apartment.Images.FirstOrDefault(i => i.IsCover)?.Url
                            ?? apartment.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditListingViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Challenge();

        if (!Enum.IsDefined(model.FurnishingLevel))
            ModelState.AddModelError(nameof(model.FurnishingLevel), "Tình trạng nội thất không hợp lệ.");
        if (!Enum.IsDefined(model.ParkingType))
            ModelState.AddModelError(nameof(model.ParkingType), "Loại chỗ đậu xe không hợp lệ.");
        if (model.HouseDirection.HasValue && !Enum.IsDefined(model.HouseDirection.Value))
            ModelState.AddModelError(nameof(model.HouseDirection), "Hướng nhà không hợp lệ.");
        if (model.AvailableFrom == default)
            ModelState.AddModelError(nameof(model.AvailableFrom), "Ngày có thể vào ở không hợp lệ.");
        if (model.MaxLeaseMonths < model.MinLeaseMonths)
            ModelState.AddModelError(nameof(model.MaxLeaseMonths), "Thời hạn thuê tối đa không hợp lệ.");

        if (!ModelState.IsValid)
        {
            // Refresh display fields trên view khi validation fail
            var orig = await _db.Apartments.AsNoTracking()
                .Include(a => a.Images)
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == model.Id && a.HostId == userId);
            if (orig != null)
            {
                model.CategoryName = orig.Category.Name;
                model.CoverImageUrl = orig.Images.FirstOrDefault(i => i.IsCover)?.Url
                                      ?? orig.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url;
            }
            return View(model);
        }

        var apartment = await _db.Apartments
            .FirstOrDefaultAsync(a => a.Id == model.Id && a.HostId == userId);
        if (apartment is null) return NotFound();

        apartment.Title = model.Title.Trim();
        apartment.Description = model.Description?.Trim() ?? apartment.Description;
        apartment.Price = model.Price;
        apartment.DefaultDeposit = model.DefaultDeposit;
        apartment.FeeNote = model.FeeNote?.Trim();
        apartment.Address = model.Address.Trim();
        apartment.Area = model.Area;
        apartment.Bedrooms = model.Bedrooms;
        apartment.Bathrooms = model.Bathrooms;
        apartment.FurnishingLevel = model.FurnishingLevel;
        apartment.AllowsPets = model.AllowsPets;
        apartment.ParkingType = model.ParkingType;
        apartment.AvailableFrom = model.AvailableFrom;
        apartment.MinLeaseMonths = model.MinLeaseMonths;
        apartment.MaxLeaseMonths = model.MaxLeaseMonths;
        apartment.HouseDirection = model.HouseDirection;
        apartment.FloorNumber = model.FloorNumber;
        apartment.Status = model.Status;
        apartment.UpdatedAt = DateTime.UtcNow;
        apartment.UpdatedBy = userId;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật tin đăng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Challenge();

        var apartment = await _db.Apartments
            .FirstOrDefaultAsync(a => a.Id == id && a.HostId == userId);
        if (apartment is null) return NotFound();

        apartment.Status = apartment.Status == ListingStatus.Active
            ? ListingStatus.Hidden
            : ListingStatus.Active;
        apartment.UpdatedAt = DateTime.UtcNow;
        apartment.UpdatedBy = userId;

        await _db.SaveChangesAsync();

        TempData["Success"] = apartment.Status == ListingStatus.Active
            ? "Tin đã được hiển thị lại."
            : "Tin đã được ẩn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Challenge();

        var apartment = await _db.Apartments
            .FirstOrDefaultAsync(a => a.Id == id && a.HostId == userId);
        if (apartment is null) return NotFound();

        // Block deletion when the apartment still has a lease in a blocking state.
        // Otherwise invoices/maintenance keep pointing at a soft-deleted apartment.
        var blockingStatuses = new[] { LeaseStatus.Active, LeaseStatus.Pending, LeaseStatus.Renewing };
        var hasBlockingLease = await _db.Leases.AnyAsync(l =>
            l.ApartmentId == id && blockingStatuses.Contains(l.Status));
        if (hasBlockingLease)
        {
            TempData["Danger"] = "Căn hộ đang có hợp đồng đang hiệu lực — không thể xoá. Vui lòng chấm dứt hợp đồng trước.";
            return RedirectToAction(nameof(Index));
        }

        apartment.IsDeleted = true;
        apartment.DeletedAt = DateTime.UtcNow;
        apartment.DeletedBy = userId;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xoá tin đăng.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Viewings(ViewingStatus? status)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Challenge();

        var query = _db.ViewingAppointments
            .AsNoTracking()
            .Include(v => v.Apartment)
            .Where(v => v.Apartment.HostId == userId);

        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);

        var items = await query
            .OrderBy(v => v.Status == ViewingStatus.Pending ? 0 : 1)
            .ThenBy(v => v.ScheduledDate)
            .ThenBy(v => v.SlotHour)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.PendingCount = await _db.ViewingAppointments
            .CountAsync(v => v.Apartment.HostId == userId && v.Status == ViewingStatus.Pending);

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmViewing(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Challenge();

        var item = await _db.ViewingAppointments
            .Include(v => v.Apartment)
            .FirstOrDefaultAsync(v => v.Id == id && v.Apartment.HostId == userId);
        if (item is null) return NotFound();

        item.Status = ViewingStatus.Confirmed;
        item.ConfirmedAt = DateTime.UtcNow;
        item.ConfirmedBy = User?.Identity?.Name;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xác nhận lịch xem phòng.";
        return RedirectToAction(nameof(Viewings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelViewing(int id, string? reason)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Challenge();

        var item = await _db.ViewingAppointments
            .Include(v => v.Apartment)
            .FirstOrDefaultAsync(v => v.Id == id && v.Apartment.HostId == userId);
        if (item is null) return NotFound();

        item.Status = ViewingStatus.Cancelled;
        item.CancelledAt = DateTime.UtcNow;
        item.CancelledBy = User?.Identity?.Name;
        item.CancellationReason = reason?.Trim();
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã huỷ lịch xem phòng.";
        return RedirectToAction(nameof(Viewings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteViewing(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null) return Challenge();

        var item = await _db.ViewingAppointments
            .Include(v => v.Apartment)
            .FirstOrDefaultAsync(v => v.Id == id && v.Apartment.HostId == userId);
        if (item is null) return NotFound();

        item.Status = ViewingStatus.Completed;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã đánh dấu lịch hẹn là đã xem.";
        return RedirectToAction(nameof(Viewings));
    }
}
