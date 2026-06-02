using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Application.Commands.Auth;
using t.Application.Commands.Listings;
using t.Application.Queries.Rentals;
using t.Data;
using t.Infrastructure.Formatting;
using t.Infrastructure.Geo;
using t.Infrastructure.Time;
using t.Models;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly CreateListingCommandHandler _createListingCommandHandler;
    private readonly AuthCommandHandler _authCommandHandler;
    private readonly RentalsQueryHandler _rentalsQueryHandler;
    private readonly NearbyApartmentRecommendationsQueryHandler _nearbyApartmentRecommendationsQueryHandler;
    private readonly RentalPreferenceProfileQueryHandler _rentalPreferenceProfileQueryHandler;

    public HomeController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        CreateListingCommandHandler createListingCommandHandler,
        AuthCommandHandler authCommandHandler,
        RentalsQueryHandler rentalsQueryHandler,
        NearbyApartmentRecommendationsQueryHandler nearbyApartmentRecommendationsQueryHandler,
        RentalPreferenceProfileQueryHandler rentalPreferenceProfileQueryHandler)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _createListingCommandHandler = createListingCommandHandler;
        _authCommandHandler = authCommandHandler;
        _rentalsQueryHandler = rentalsQueryHandler;
        _nearbyApartmentRecommendationsQueryHandler = nearbyApartmentRecommendationsQueryHandler;
        _rentalPreferenceProfileQueryHandler = rentalPreferenceProfileQueryHandler;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.FeaturedApartments = await _db.Apartments
            .AsNoTracking()
            .Include(a => a.Images)
            .Include(a => a.Region)
            .Where(a => a.Status == ListingStatus.Active && a.IsFeatured)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Take(4)
            .ToListAsync();

        var heroSlugs = new[]
        {
            t.Data.SampleHeroes.LandmarkSlug,
            t.Data.SampleHeroes.OceanParkSlug,
            t.Data.SampleHeroes.SunGroupSlug
        };
        var heroList = await _db.Apartments
            .AsNoTracking()
            .Include(a => a.Images)
            .Include(a => a.Region)
            .Where(a => heroSlugs.Contains(a.Slug))
            .ToListAsync();
        ViewBag.HeroLandmark = heroList.FirstOrDefault(a => a.Slug == t.Data.SampleHeroes.LandmarkSlug);
        ViewBag.HeroOceanPark = heroList.FirstOrDefault(a => a.Slug == t.Data.SampleHeroes.OceanParkSlug);
        ViewBag.HeroSunGroup = heroList.FirstOrDefault(a => a.Slug == t.Data.SampleHeroes.SunGroupSlug);

        ViewBag.Regions = await _db.Regions.AsNoTracking().ToListAsync();
        ViewBag.Categories = await _db.Categories.AsNoTracking().ToListAsync();

        return View();
    }

    public IActionResult Privacy() => View();

    public async Task<IActionResult> Rentals([FromQuery] RentalSearchRequest request)
    {
        const int pageSize = 12;
        request.PageSize = pageSize;
        var hasCoordinateBindingError =
            HasModelStateErrors(nameof(request.Latitude)) ||
            HasModelStateErrors(nameof(request.Longitude));
        var coordinates = GeoDistance.ValidatePair(request.Latitude, request.Longitude);
        if (hasCoordinateBindingError || !coordinates.IsValid)
        {
            TempData["Warning"] = "Vị trí không hợp lệ. Danh sách đang hiển thị theo thứ tự mặc định.";
            request.Latitude = null;
            request.Longitude = null;
            if (request.Sort == "distance_asc")
                request.Sort = null;
        }

        var normalization = RentalPreferenceNormalizer.Normalize(request, strict: false);
        if (!normalization.IsValid)
        {
            TempData["Warning"] = normalization.Errors[0];
            if (request.Sort == "match_desc")
                request.Sort = null;
        }
        else if (normalization.Warnings.Count > 0)
        {
            TempData["Warning"] = normalization.Warnings[0];
        }

        var model = await _rentalsQueryHandler.SearchAsync(request);
        model.Search = request;
        var userId = _userManager.GetUserId(User);
        if (userId != null)
            model.SavedPreference = await _rentalPreferenceProfileQueryHandler.GetAsync(userId);

        ViewData["ActiveCategorySlug"] = request.Category;

        ViewBag.Categories = await _db.Categories.AsNoTracking().ToListAsync();
        ViewBag.Amenities = await _db.Amenities.AsNoTracking().ToListAsync();
        ViewBag.Regions = await _db.Regions.AsNoTracking().ToListAsync();
        ViewBag.FavoriteIds = await GetFavoriteIdsAsync();

        return View(model);
    }

    private bool HasModelStateErrors(string key)
    {
        return ModelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0;
    }

    private async Task<HashSet<int>> GetFavoriteIdsAsync()
    {
        var uid = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(uid)) return new HashSet<int>();
        var ids = await _db.Favorites.AsNoTracking()
            .Where(f => f.UserId == uid)
            .Select(f => f.ApartmentId)
            .ToListAsync();
        return ids.ToHashSet();
    }

    public async Task<IActionResult> ApartmentDetail(int id = 1)
    {
        var apartment = await _db.Apartments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Images.OrderBy(i => i.SortOrder))
            .Include(a => a.Region)
            .Include(a => a.Category)
            .Include(a => a.Host)
            .Include(a => a.Project)
            .Include(a => a.ApartmentAmenities).ThenInclude(aa => aa.Amenity)
            .Include(a => a.Reviews.Where(r => r.Status == ReviewStatus.Approved)).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (apartment == null) return NotFound();

        var similar = await _nearbyApartmentRecommendationsQueryHandler.GetAsync(apartment);

        var cover = apartment.Images.FirstOrDefault(i => i.IsCover) ?? apartment.Images.FirstOrDefault();
        var sideImages = apartment.Images.Where(i => !i.IsCover).Take(3).ToList();

        var vm = new ApartmentDetailPageViewModel
        {
            Apartment = new ApartmentDetailViewModel
            {
                Id = apartment.Id,
                Title = apartment.Title,
                Location = apartment.Address,
                Area = $"{apartment.Area} m2",
                Rating = apartment.Reviews.Any()
                    ? apartment.Reviews.Average(r => r.Rating).ToString("0.0")
                    : "N/A",
                ReviewSummary = $"{apartment.Reviews.Count} đánh giá",
                Price = RentalPriceFormatter.Format(apartment.Price),
                FeeNote = apartment.FeeNote ?? string.Empty,
                MainImage = cover?.Url ?? string.Empty,
                SideImageOne = sideImages.ElementAtOrDefault(0)?.Url ?? string.Empty,
                SideImageTwo = sideImages.ElementAtOrDefault(1)?.Url ?? string.Empty,
                SideImageThree = sideImages.ElementAtOrDefault(2)?.Url ?? string.Empty,
                MapImage = "/img/detail/map.jpg",
                DescriptionOne = apartment.Description ?? string.Empty,
                DescriptionTwo = apartment.DescriptionExtra ?? string.Empty
            },
            SimilarApartments = similar
        };

        ViewBag.Entity = apartment;
        ViewBag.FavoriteIds = await GetFavoriteIdsAsync();

        // Review eligibility: signed-in user who has rented this apartment (any non-Pending lease).
        var uid = _userManager.GetUserId(User);
        if (!string.IsNullOrEmpty(uid))
        {
            ViewBag.CanReview = await _db.Leases.AnyAsync(l =>
                l.ApartmentId == id &&
                (l.PrimaryTenantId == uid || l.AdditionalTenants.Any(t => t.TenantId == uid)) &&
                l.Status != LeaseStatus.Pending);
            ViewBag.MyReview = await _db.Reviews.AsNoTracking()
                .FirstOrDefaultAsync(r => r.ApartmentId == id && r.UserId == uid);
        }
        return View(vm);
    }

    [Authorize]
    public async Task<IActionResult> PostListing()
    {
        var model = await BuildPostListingViewModelAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> PostListing(CreateApartmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulatePostListingLookupsAsync(model);
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var result = await _createListingCommandHandler.HandleAsync(new CreateListingCommand
        {
            Model = model,
            HostId = userId
        });

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Key, error.Message);
            }

            await PopulatePostListingLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(ApartmentDetail), new { id = result.ApartmentId });
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated == true)
            await _signInManager.SignOutAsync();

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        if (User?.Identity?.IsAuthenticated == true)
            await _signInManager.SignOutAsync();

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin") || roles.Contains("Manager"))
                    return LocalRedirect("/admin");
                if (roles.Contains("Tenant"))
                    return LocalRedirect("/tenant");
            }
            return LocalRedirect("/");
        }

        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Phone = model.Phone,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Public sign-up always lands in the Tenant role. Without this the user
            // cannot access /tenant area and admin lease dropdowns won't list them.
            await _userManager.AddToRoleAsync(user, "Tenant");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            var message = ToVietnameseIdentityError(error.Code);
            if (error.Code is "DuplicateEmail" or "InvalidEmail")
            {
                ModelState.AddModelError(nameof(model.Email), message);
                continue;
            }

            if (error.Code.StartsWith("Password", StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.Password), message);
                continue;
            }

            ModelState.AddModelError(string.Empty, message);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _authCommandHandler.ForgotPasswordAsync(model.Email);
        TempData["ForgotSuccess"] = true;
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return BadRequest();

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authCommandHandler.ResetPasswordAsync(model);
        if (result.Succeeded)
        {
            TempData["ResetSuccess"] = true;
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        Response.Cookies.Delete(".AspNetCore.Identity.Application");
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookViewing(BookViewingViewModel model)
    {
        var apartment = await _db.Apartments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == model.ApartmentId && a.Status == ListingStatus.Active);

        if (apartment is null)
        {
            TempData["Danger"] = "Tin đăng không tồn tại hoặc đã ẩn.";
            return RedirectToAction(nameof(Rentals));
        }

        var today = VnTime.Today;
        if (model.ScheduledDate.Date < today)
            ModelState.AddModelError(nameof(model.ScheduledDate), "Vui lòng chọn ngày từ hôm nay trở đi.");

        if (model.ScheduledDate.Date > today.AddMonths(2))
            ModelState.AddModelError(nameof(model.ScheduledDate), "Chỉ đặt lịch trong vòng 2 tháng.");

        if (!BookViewingViewModel.AvailableHours().Contains(model.SlotHour))
            ModelState.AddModelError(nameof(model.SlotHour), "Khung giờ không hợp lệ.");

        // Slot collision: same apartment + same date + same hour + not-cancelled.
        var slotTaken = await _db.ViewingAppointments.AnyAsync(v =>
            v.ApartmentId == model.ApartmentId &&
            v.ScheduledDate == model.ScheduledDate.Date &&
            v.SlotHour == model.SlotHour &&
            v.Status != ViewingStatus.Cancelled);
        if (slotTaken)
            ModelState.AddModelError(nameof(model.SlotHour), "Khung giờ này đã có người đặt. Vui lòng chọn giờ khác.");

        if (!ModelState.IsValid)
        {
            TempData["Danger"] = string.Join(" · ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(ApartmentDetail), new { id = model.ApartmentId });
        }

        var userId = _userManager.GetUserId(User);

        var appointment = new Models.Entities.ViewingAppointment
        {
            ApartmentId = model.ApartmentId,
            UserId = userId,
            ContactName = model.ContactName.Trim(),
            ContactPhone = model.ContactPhone.Trim(),
            ContactEmail = model.ContactEmail?.Trim(),
            ScheduledDate = model.ScheduledDate.Date,
            SlotHour = model.SlotHour,
            Note = model.Note?.Trim(),
            Status = Models.Entities.ViewingStatus.Pending
        };
        _db.ViewingAppointments.Add(appointment);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã đặt lịch xem phòng vào {appointment.ScheduledDate:dd/MM/yyyy} lúc {appointment.SlotHour:00}:00. Chủ tin sẽ xác nhận sớm.";
        return RedirectToAction(nameof(ApartmentDetail), new { id = model.ApartmentId });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<CreateApartmentViewModel> BuildPostListingViewModelAsync()
    {
        var model = new CreateApartmentViewModel();
        await PopulatePostListingLookupsAsync(model);
        model.CoverImageIndex = 0;
        return model;
    }

    private async Task PopulatePostListingLookupsAsync(CreateApartmentViewModel model)
    {
        model.Categories = await _db.Categories.AsNoTracking().ToListAsync();
        model.Regions = await _db.Regions.AsNoTracking().ToListAsync();
        model.Amenities = await _db.Amenities.AsNoTracking().ToListAsync();
        model.Projects = await _db.Projects.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    private static string ToVietnameseIdentityError(string code)
    {
        return code switch
        {
            "DuplicateEmail" => "Email này đã được đăng ký. Vui lòng dùng email khác.",
            "DuplicateUserName" => "Tài khoản đã tồn tại. Vui lòng kiểm tra lại email.",
            "InvalidEmail" => "Email không đúng định dạng.",
            "PasswordTooShort" => "Mật khẩu phải có ít nhất 6 ký tự.",
            "PasswordRequiresDigit" => "Mật khẩu phải chứa ít nhất 1 chữ số.",
            "PasswordRequiresUpper" => "Mật khẩu phải chứa ít nhất 1 chữ cái in hoa.",
            "PasswordRequiresLower" => "Mật khẩu phải chứa ít nhất 1 chữ cái thường.",
            "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt.",
            _ => "Không thể tạo tài khoản. Vui lòng kiểm tra lại thông tin đã nhập."
        };
    }
}
