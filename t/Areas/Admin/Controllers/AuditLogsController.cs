using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

[t.Infrastructure.Security.Permission("audit.view")]
public class AuditLogsController : AdminBaseController
{
    public AuditLogsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? entity, AuditAction? action, string? user, DateTime? from, DateTime? to, int page = 1)
    {
        SetActiveNav("audit");
        SetBreadcrumb(("Audit log", null));

        const int pageSize = 30;
        var query = Db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(l => l.EntityName == entity);
        if (action.HasValue) query = query.Where(l => l.Action == action.Value);
        if (!string.IsNullOrWhiteSpace(user)) query = query.Where(l => l.UserName != null && l.UserName.Contains(user));
        // Ngày lọc là ngày giờ Việt Nam, cột Timestamp lưu UTC; "đến ngày" tính trọn ngày đó.
        if (from.HasValue) { var f = t.Infrastructure.Time.VnTime.ToUtc(from.Value.Date); query = query.Where(l => l.Timestamp >= f); }
        if (to.HasValue) { var e = t.Infrastructure.Time.VnTime.ToUtc(to.Value.Date.AddDays(1)); query = query.Where(l => l.Timestamp < e); }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new AuditListVm
            {
                Id = l.Id, EntityName = l.EntityName, EntityKey = l.EntityKey,
                Action = l.Action, UserName = l.UserName, Timestamp = l.Timestamp,
                IpAddress = l.IpAddress, ChangedColumns = l.ChangedColumns
            }).ToListAsync();

        ViewBag.Entity = entity; ViewBag.Action = action; ViewBag.User = user;
        ViewBag.From = from; ViewBag.To = to;
        ViewBag.EntityNames = await Db.AuditLogs.Select(l => l.EntityName).Distinct().OrderBy(x => x).ToListAsync();
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { entity, action, user, from, to, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Details(long id)
    {
        SetActiveNav("audit");
        var log = await Db.AuditLogs.FirstOrDefaultAsync(l => l.Id == id);
        if (log == null) return NotFound();
        SetBreadcrumb(("Audit log", Url.Action(nameof(Index))), ($"#{id}", null));
        return View(log);
    }
}
