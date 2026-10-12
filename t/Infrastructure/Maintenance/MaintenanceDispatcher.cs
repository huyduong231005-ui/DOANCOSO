using System.Net;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Ai;
using t.Infrastructure.Email;
using t.Infrastructure.Localization;
using t.Models.Entities;

namespace t.Infrastructure.Maintenance;

/// <summary>
/// Sau khi yêu cầu sự cố được lưu: chọn thợ theo loại sự cố, gán và gửi email cho thợ.
/// Quản lý luôn nhận email; không có thợ phù hợp hoặc mô tả mơ hồ thì yêu cầu để quản lý phân công.
/// Mọi lỗi chỉ ghi log: yêu cầu của cư dân đã lưu, không được hỏng vì email.
/// </summary>
public class MaintenanceDispatcher
{
    private static readonly MaintenanceStatus[] ActiveStatuses =
        { MaintenanceStatus.Open, MaintenanceStatus.Acknowledged, MaintenanceStatus.InProgress };

    private static readonly string[] ManagerRoles = { SeedData.RoleAdmin, SeedData.RoleManager };

    // ponytail: mô hình chỉ có một loại "structural" nên tách thợ mộc / nhôm kính bằng từ khóa.
    // Sai khi mô tả không dấu dùng "kinh" theo nghĩa khác ("kinh khung"); muốn chắc thì thêm
    // loại riêng vào prompt và train lại.
    private static readonly string[] GlassWords = { "kinh", "nhom", "cua-so", "cua-lua" };

    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<MaintenanceDispatcher> _log;

    public MaintenanceDispatcher(AppDbContext db, IEmailSender email, ILogger<MaintenanceDispatcher> log)
    {
        _db = db;
        _email = email;
        _log = log;
    }

    /// <summary>Role của thợ xử lý loại sự cố này. Null = không có thợ chuyên trách, quản lý xử lý.</summary>
    public static string? RoleFor(MaintenanceCategory category, string description) => category switch
    {
        MaintenanceCategory.Electrical => SeedData.RoleElectrician,
        MaintenanceCategory.Plumbing => SeedData.RolePlumber,
        MaintenanceCategory.Appliance => SeedData.RoleAcTechnician,
        MaintenanceCategory.Structural => IsGlassWork(description) ? SeedData.RoleGlazier : SeedData.RoleCarpenter,
        _ => null,
    };

    public async Task DispatchAsync(MaintenanceRequest req, MaintenanceTriage? triage, CancellationToken ct = default)
    {
        try
        {
            // Mô tả mơ hồ thì loại do mô hình đoán không đáng tin: để quản lý hỏi lại trước khi giao thợ.
            var role = triage?.FollowUpQuestion == null ? RoleFor(req.Category, req.Description) : null;
            var assignee = role == null ? null : await LeastBusyAsync(role, ct);
            if (assignee != null)
            {
                req.AssignedToId = assignee.Id;
                await _db.SaveChangesAsync(ct);
            }

            var recipients = await UsersInRolesAsync(ManagerRoles, ct);
            if (assignee != null) recipients.Add(assignee);

            await _db.Entry(req).Reference(r => r.Apartment).LoadAsync(ct);
            await _db.Entry(req).Reference(r => r.Reporter).LoadAsync(ct);
            var subject = BuildSubject(req);
            var html = BuildHtml(req, triage, assignee);
            foreach (var to in recipients.Select(u => u.Email).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct())
            {
                try { await _email.SendAsync(to!, subject, html, ct); }
                catch (Exception ex) { _log.LogWarning(ex, "Could not email {To} about {RequestNumber}", to, req.RequestNumber); }
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Maintenance dispatch failed for {RequestNumber}", req.RequestNumber);
        }
    }

    private static bool IsGlassWork(string description)
    {
        var slug = "-" + Slugify.Make(description) + "-";
        return GlassWords.Any(w => slug.Contains("-" + w + "-"));
    }

    private async Task<AppUser?> LeastBusyAsync(string role, CancellationToken ct)
    {
        var staff = await UsersInRolesAsync(new[] { role }, ct);
        if (staff.Count == 0) return null;
        var ids = staff.Select(s => s.Id).ToList();
        var load = await _db.MaintenanceRequests
            .Where(m => m.AssignedToId != null && ids.Contains(m.AssignedToId) && ActiveStatuses.Contains(m.Status))
            .GroupBy(m => m.AssignedToId!)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        return staff.OrderBy(s => load.GetValueOrDefault(s.Id)).ThenBy(s => s.Id).First();
    }

    private Task<List<AppUser>> UsersInRolesAsync(string[] roles, CancellationToken ct) =>
        (from u in _db.Users
         join ur in _db.UserRoles on u.Id equals ur.UserId
         join r in _db.Roles on ur.RoleId equals r.Id
         where roles.Contains(r.Name)
         select u).Distinct().ToListAsync(ct);

    private static string BuildSubject(MaintenanceRequest req)
    {
        var title = req.Title.ReplaceLineEndings(" ");
        var prefix = req.Priority == MaintenancePriority.Urgent ? "KHẨN CẤP - " : "";
        return $"[Luxe Haven] {prefix}Sự cố {req.RequestNumber}: {title}";
    }

    private static string BuildHtml(MaintenanceRequest req, MaintenanceTriage? triage, AppUser? assignee)
    {
        static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        var reporter = req.Reporter;
        var phone = reporter?.Phone ?? reporter?.PhoneNumber;
        var danger = triage?.IsDangerous == true
            ? "<p><strong>Cảnh báo: sự cố có nguy cơ gây mất an toàn.</strong></p>" : "";
        var question = triage?.FollowUpQuestion != null
            ? $"<p>Mô tả chưa rõ, cần hỏi lại cư dân: <em>{E(triage.FollowUpQuestion)}</em></p>" : "";
        var owner = assignee != null
            ? $"Đã giao cho: <strong>{E(assignee.FullName)}</strong>"
            : "<strong>Chưa giao cho thợ nào</strong>, cần quản lý phân công.";
        return $@"
<p>Yêu cầu <strong>{E(req.RequestNumber)}</strong> · {E(req.Category.Vi())} · Ưu tiên: <strong>{E(req.Priority.Vi())}</strong></p>
{danger}
<p>Căn hộ: <strong>{E(req.Apartment?.Title)}</strong><br/>
Người báo: {E(reporter?.FullName)} · {E(phone)}</p>
<p><strong>{E(req.Title)}</strong></p>
<p style=""white-space:pre-wrap"">{E(req.Description)}</p>
{question}
<p>{owner}</p>
<p>— Luxe Haven</p>";
    }
}
