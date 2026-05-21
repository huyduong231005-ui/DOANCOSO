using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class UsersController : AdminBaseController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(AppDbContext db, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager) : base(db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(string? q, string? role, int page = 1)
    {
        SetActiveNav("users");
        SetBreadcrumb(("Người dùng", null));

        const int pageSize = 25;
        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => u.Email!.Contains(q) || u.FullName.Contains(q) || u.PhoneNumber!.Contains(q));

        var users = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        var total = await query.CountAsync();

        var rows = new List<UserListVm>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            if (!string.IsNullOrEmpty(role) && !roles.Contains(role)) continue;
            var leases = await Db.Leases.CountAsync(l => l.PrimaryTenantId == u.Id);
            rows.Add(new UserListVm
            {
                Id = u.Id, Email = u.Email!, FullName = u.FullName,
                Phone = u.Phone, IsHost = u.IsHost,
                LockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
                CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt,
                Roles = roles.ToList(), LeasesCount = leases
            });
        }

        ViewBag.Q = q; ViewBag.Role = role;
        ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { q, role, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Edit(string id)
    {
        SetActiveNav("users");
        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return NotFound();
        SetBreadcrumb(("Người dùng", Url.Action(nameof(Index))), (u.FullName, null));

        var roles = await _userManager.GetRolesAsync(u);
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        var vm = new UserEditVm
        {
            Id = u.Id, Email = u.Email!, FullName = u.FullName,
            Phone = u.Phone, IsHost = u.IsHost, HostTitle = u.HostTitle,
            LockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
            SelectedRoles = roles.ToList(), AllRoles = allRoles
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditVm input)
    {
        if (!ModelState.IsValid)
        {
            input.AllRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            return View(input);
        }

        var u = await _userManager.FindByIdAsync(input.Id);
        if (u == null) return NotFound();

        u.FullName = input.FullName;
        u.Phone = input.Phone;
        u.PhoneNumber = input.Phone;
        u.IsHost = input.IsHost;
        u.HostTitle = input.HostTitle;

        if (!string.Equals(u.Email, input.Email, StringComparison.OrdinalIgnoreCase))
        {
            u.Email = input.Email;
            u.UserName = input.Email;
            u.NormalizedEmail = input.Email.ToUpperInvariant();
            u.NormalizedUserName = input.Email.ToUpperInvariant();
        }

        await _userManager.UpdateAsync(u);

        var current = await _userManager.GetRolesAsync(u);
        var toAdd = input.SelectedRoles.Except(current);
        var toRemove = current.Except(input.SelectedRoles);
        if (toAdd.Any()) await _userManager.AddToRolesAsync(u, toAdd);
        if (toRemove.Any()) await _userManager.RemoveFromRolesAsync(u, toRemove);

        TempData["Success"] = "Đã cập nhật người dùng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return NotFound();
        var locked = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow;
        await _userManager.SetLockoutEnabledAsync(u, true);
        await _userManager.SetLockoutEndDateAsync(u, locked ? null : DateTimeOffset.UtcNow.AddYears(50));
        TempData["Success"] = locked ? "Đã mở khoá tài khoản." : "Đã khoá tài khoản.";
        return RedirectToAction(nameof(Index));
    }
}
