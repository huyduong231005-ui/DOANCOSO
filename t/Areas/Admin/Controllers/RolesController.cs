using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class RolesController : AdminBaseController
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolesController(AppDbContext db, RoleManager<IdentityRole> roleManager) : base(db)
    {
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        SetActiveNav("roles");
        SetBreadcrumb(("Vai trò & quyền", null));

        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        var perms = await Db.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Code).ToListAsync();
        var rps = await Db.RolePermissions.ToListAsync();

        var matrix = new RoleMatrixVm
        {
            Roles = roles.Select(r => r.Name!).ToList()
        };

        foreach (var grp in perms.GroupBy(p => p.Module))
        {
            var rows = new List<PermissionRow>();
            foreach (var p in grp)
            {
                var row = new PermissionRow { Id = p.Id, Code = p.Code, DisplayName = p.DisplayName };
                foreach (var role in roles)
                {
                    row.Assignments[role.Name!] = rps.Any(rp => rp.RoleId == role.Id && rp.PermissionId == p.Id);
                }
                rows.Add(row);
            }
            matrix.ByModule[grp.Key] = rows;
        }

        return View(matrix);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(IFormCollection form)
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var perms = await Db.Permissions.ToListAsync();
        var existing = await Db.RolePermissions.ToListAsync();

        Db.RolePermissions.RemoveRange(existing);

        foreach (var role in roles)
        {
            foreach (var p in perms)
            {
                var key = $"perm_{role.Id}_{p.Id}";
                if (form.ContainsKey(key) && form[key].Contains("on"))
                    Db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
            }
        }

        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu phân quyền.";
        return RedirectToAction(nameof(Index));
    }
}
