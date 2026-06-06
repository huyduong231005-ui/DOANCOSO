using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Localization;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class ApartmentsController : AdminBaseController
{
    private readonly UserManager<AppUser> _userManager;

    public ApartmentsController(AppDbContext db, UserManager<AppUser> userManager) : base(db)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? q, ListingStatus? status, ApartmentOccupancy? occupancy, int? regionId, int? categoryId, int? buildingId, int page = 1)
    {
        SetActiveNav("apartments");
        SetBreadcrumb(("Căn hộ", null));

        const int pageSize = 15;
        var query = Db.Apartments
            .Include(a => a.Region)
            .Include(a => a.Category)
            .Include(a => a.Host)
            .Include(a => a.Building)
            .Include(a => a.Floor)
            .Include(a => a.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => a.Title.Contains(q) || a.Address.Contains(q) || a.Slug.Contains(q) || (a.UnitCode != null && a.UnitCode.Contains(q)));
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);
        if (occupancy.HasValue)
            query = query.Where(a => a.Occupancy == occupancy.Value);
        if (regionId.HasValue)
            query = query.Where(a => a.RegionId == regionId.Value);
        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);
        if (buildingId.HasValue)
            query = query.Where(a => a.BuildingId == buildingId.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ApartmentListVm
            {
                Id = a.Id,
                Title = a.Title,
                UnitCode = a.UnitCode,
                CoverUrl = a.Images.Where(i => i.IsCover).Select(i => i.Url).FirstOrDefault()
                           ?? a.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()
                           ?? string.Empty,
                Price = a.Price,
                RegionName = a.Region.Name,
                CategoryName = a.Category.Name,
                BuildingName = a.Building != null ? a.Building.Name : null,
                FloorLabel = a.Floor != null ? a.Floor.Label : null,
                HostName = a.Host.FullName,
                Status = a.Status,
                Occupancy = a.Occupancy,
                IsFeatured = a.IsFeatured,
                ViewCount = a.ViewCount,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        ViewBag.Regions = await Db.Regions.OrderBy(r => r.Name).ToListAsync();
        ViewBag.Categories = await Db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Buildings = await Db.Buildings.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.Occupancy = occupancy;
        ViewBag.RegionId = regionId;
        ViewBag.CategoryId = categoryId;
        ViewBag.BuildingId = buildingId;

        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = p => Url.Action(nameof(Index), new { q, status, occupancy, regionId, categoryId, buildingId, page = p }) ?? "#"
        };

        return View(rows);
    }

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var rows = await Db.Apartments
            .Include(a => a.Images)
            .Include(a => a.Building)
            .Where(a => a.Title.Contains(q) || a.Address.Contains(q) || a.Slug.Contains(q) || (a.UnitCode != null && a.UnitCode.Contains(q)))
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new
            {
                title = (a.UnitCode != null ? a.UnitCode + " · " : "") + a.Title,
                subtitle = (a.Building != null ? a.Building.Name + " · " : "") + a.Address,
                url = Url.Action(nameof(Details), new { id = a.Id }) ?? "#",
                thumb = a.Images.Where(i => i.IsCover).Select(i => i.Url).FirstOrDefault()
                        ?? a.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                icon = "apartment"
            })
            .ToListAsync();
        return Json(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("apartments");
        var apt = await Db.Apartments
            .Include(a => a.Region).Include(a => a.Category).Include(a => a.Host).Include(a => a.Project)
            .Include(a => a.Images)
            .Include(a => a.ApartmentAmenities).ThenInclude(aa => aa.Amenity)
            .Include(a => a.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (apt == null) return NotFound();

        SetBreadcrumb(("Căn hộ", Url.Action(nameof(Index))), (apt.Title, null));
        return View(apt);
    }

    public async Task<IActionResult> Create()
    {
        SetActiveNav("apartments");
        SetBreadcrumb(("Căn hộ", Url.Action(nameof(Index))), ("Thêm mới", null));

        var vm = new ApartmentEditVm
        {
            Status = ListingStatus.Draft,
            Bedrooms = 1, Bathrooms = 1
        };
        await PopulateLookupsAsync(vm);
        return View("Edit", vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        SetActiveNav("apartments");
        var apt = await Db.Apartments
            .Include(a => a.ApartmentAmenities)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (apt == null) return NotFound();

        SetBreadcrumb(("Căn hộ", Url.Action(nameof(Index))), ($"#{apt.Id}", Url.Action(nameof(Details), new { id })), ("Chỉnh sửa", null));

        var vm = new ApartmentEditVm
        {
            Id = apt.Id,
            Title = apt.Title, Slug = apt.Slug, UnitCode = apt.UnitCode,
            Description = apt.Description, DescriptionExtra = apt.DescriptionExtra,
            Price = apt.Price, DefaultDeposit = apt.DefaultDeposit, FeeNote = apt.FeeNote,
            Area = apt.Area, Bedrooms = apt.Bedrooms, Bathrooms = apt.Bathrooms,
            Address = apt.Address, Latitude = apt.Latitude, Longitude = apt.Longitude,
            FurnishingLevel = apt.FurnishingLevel, AllowsPets = apt.AllowsPets,
            ParkingType = apt.ParkingType, AvailableFrom = apt.AvailableFrom,
            MinLeaseMonths = apt.MinLeaseMonths, MaxLeaseMonths = apt.MaxLeaseMonths,
            HouseDirection = apt.HouseDirection, FloorNumber = apt.FloorNumber,
            Status = apt.Status, Occupancy = apt.Occupancy, IsFeatured = apt.IsFeatured,
            HostId = apt.HostId, RegionId = apt.RegionId, CategoryId = apt.CategoryId,
            ProjectId = apt.ProjectId, BuildingId = apt.BuildingId, FloorId = apt.FloorId,
            AmenityIds = apt.ApartmentAmenities.Select(aa => aa.AmenityId).ToList()
        };
        await PopulateLookupsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ApartmentEditVm input)
    {
        if (!Enum.IsDefined(input.FurnishingLevel))
            ModelState.AddModelError(nameof(input.FurnishingLevel), "Tình trạng nội thất không hợp lệ.");
        if (!Enum.IsDefined(input.ParkingType))
            ModelState.AddModelError(nameof(input.ParkingType), "Loại chỗ đậu xe không hợp lệ.");
        if (input.HouseDirection.HasValue && !Enum.IsDefined(input.HouseDirection.Value))
            ModelState.AddModelError(nameof(input.HouseDirection), "Hướng nhà không hợp lệ.");
        if (input.AvailableFrom == default)
            ModelState.AddModelError(nameof(input.AvailableFrom), "Ngày có thể vào ở không hợp lệ.");
        if (input.MaxLeaseMonths < input.MinLeaseMonths)
            ModelState.AddModelError(nameof(input.MaxLeaseMonths), "Thời hạn thuê tối đa không hợp lệ.");

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(input);
            return View(input);
        }

        if (string.IsNullOrWhiteSpace(input.Slug)) input.Slug = Slugify(input.Title);

        var slugExists = await Db.Apartments.AnyAsync(a => a.Slug == input.Slug && a.Id != input.Id);
        if (slugExists)
        {
            ModelState.AddModelError(nameof(input.Slug), "Slug đã tồn tại.");
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
            apt = await Db.Apartments
                .Include(a => a.ApartmentAmenities)
                .FirstOrDefaultAsync(a => a.Id == input.Id) ?? throw new InvalidOperationException("Not found");
        }

        apt.Title = input.Title;
        apt.Slug = input.Slug;
        apt.UnitCode = input.UnitCode;
        apt.Description = input.Description;
        apt.DescriptionExtra = input.DescriptionExtra;
        apt.Price = input.Price;
        apt.DefaultDeposit = input.DefaultDeposit;
        apt.FeeNote = input.FeeNote;
        apt.Area = input.Area;
        apt.Bedrooms = input.Bedrooms;
        apt.Bathrooms = input.Bathrooms;
        apt.Address = input.Address;
        apt.Latitude = input.Latitude;
        apt.Longitude = input.Longitude;
        apt.FurnishingLevel = input.FurnishingLevel;
        apt.AllowsPets = input.AllowsPets;
        apt.ParkingType = input.ParkingType;
        apt.AvailableFrom = input.AvailableFrom;
        apt.MinLeaseMonths = input.MinLeaseMonths;
        apt.MaxLeaseMonths = input.MaxLeaseMonths;
        apt.HouseDirection = input.HouseDirection;
        apt.FloorNumber = input.FloorNumber;
        apt.Status = input.Status;
        apt.Occupancy = input.Occupancy;
        apt.IsFeatured = input.IsFeatured;
        apt.HostId = input.HostId;
        apt.RegionId = input.RegionId;
        apt.CategoryId = input.CategoryId;
        apt.ProjectId = input.ProjectId;
        apt.BuildingId = input.BuildingId;
        apt.FloorId = input.FloorId;

        var existingAmenities = apt.ApartmentAmenities?.ToList() ?? new List<ApartmentAmenity>();
        var toRemove = existingAmenities.Where(aa => !input.AmenityIds.Contains(aa.AmenityId)).ToList();
        foreach (var r in toRemove) Db.ApartmentAmenities.Remove(r);
        foreach (var newId in input.AmenityIds.Where(id => !existingAmenities.Any(e => e.AmenityId == id)))
            Db.ApartmentAmenities.Add(new ApartmentAmenity { Apartment = apt, AmenityId = newId });

        await Db.SaveChangesAsync();

        TempData["Success"] = input.Id == 0 ? "Đã tạo căn hộ mới." : "Đã cập nhật căn hộ.";
        return RedirectToAction(nameof(Details), new { id = apt.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeature(int id)
    {
        var apt = await Db.Apartments.FindAsync(id);
        if (apt == null) return NotFound();
        apt.IsFeatured = !apt.IsFeatured;
        await Db.SaveChangesAsync();
        TempData["Success"] = apt.IsFeatured ? "Đã đánh dấu nổi bật." : "Đã bỏ nổi bật.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, ListingStatus status, string? returnUrl)
    {
        var apt = await Db.Apartments.FindAsync(id);
        if (apt == null) return NotFound();
        apt.Status = status;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã đổi trạng thái → {status.Vi()}.";
        return RedirectToLocalOrDefault(returnUrl, Url.Action(nameof(Details), new { id })!);
    }

    public async Task<IActionResult> Pending()
    {
        SetActiveNav("apartments");
        SetBreadcrumb(("Căn hộ", Url.Action(nameof(Index))), ("Chờ duyệt", null));

        var rows = await Db.Apartments
            .Include(a => a.Host)
            .Include(a => a.Region)
            .Include(a => a.Category)
            .Include(a => a.Images)
            .Where(a => a.Status == ListingStatus.Draft)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? returnUrl)
    {
        var apt = await Db.Apartments.FindAsync(id);
        if (apt == null) return NotFound();
        if (apt.Status != ListingStatus.Draft)
        {
            TempData["Danger"] = $"Chỉ duyệt được tin ở trạng thái Nháp (hiện tại: {apt.Status.Vi()}).";
            return RedirectToLocalOrDefault(returnUrl, Url.Action(nameof(Pending))!);
        }
        apt.Status = ListingStatus.Active;
        apt.ApprovedAt = DateTime.UtcNow;
        apt.ApprovedBy = User.Identity?.Name;
        apt.ModerationNote = null;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã duyệt tin {apt.Title}.";
        return RedirectToLocalOrDefault(returnUrl, Url.Action(nameof(Pending))!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason, string? returnUrl)
    {
        var apt = await Db.Apartments.FindAsync(id);
        if (apt == null) return NotFound();
        if (apt.Status != ListingStatus.Draft)
        {
            TempData["Danger"] = $"Chỉ từ chối được tin ở trạng thái Nháp (hiện tại: {apt.Status.Vi()}).";
            return RedirectToLocalOrDefault(returnUrl, Url.Action(nameof(Pending))!);
        }
        apt.Status = ListingStatus.Hidden;
        apt.ModerationNote = string.IsNullOrWhiteSpace(reason) ? "Không đạt tiêu chuẩn nội dung." : reason.Trim();
        apt.ApprovedAt = null;
        apt.ApprovedBy = null;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã từ chối tin {apt.Title}.";
        return RedirectToLocalOrDefault(returnUrl, Url.Action(nameof(Pending))!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var apt = await Db.Apartments.FindAsync(id);
        if (apt == null) return NotFound();
        Db.Apartments.Remove(apt);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá căn hộ (soft delete).";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(ApartmentEditVm vm)
    {
        vm.Regions = await Db.Regions.OrderBy(r => r.Name).ToListAsync();
        vm.Categories = await Db.Categories.OrderBy(c => c.Name).ToListAsync();
        vm.Projects = await Db.Projects.OrderBy(p => p.Name).ToListAsync();
        vm.Buildings = await Db.Buildings.OrderBy(b => b.Name).ToListAsync();
        vm.Floors = vm.BuildingId.HasValue
            ? await Db.Floors.Where(f => f.BuildingId == vm.BuildingId).OrderBy(f => f.Number).ToListAsync()
            : await Db.Floors.OrderBy(f => f.Number).ToListAsync();
        vm.Amenities = await Db.Amenities.OrderBy(a => a.Name).ToListAsync();
        vm.Hosts = await _userManager.Users
            .Where(u => u.IsHost)
            .OrderBy(u => u.FullName)
            .Select(u => new HostOption { Id = u.Id, Label = u.FullName + " · " + u.Email })
            .ToListAsync();
    }
}
