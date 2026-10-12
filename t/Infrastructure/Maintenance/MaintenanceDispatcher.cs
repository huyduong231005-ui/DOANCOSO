using System.Net;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Ai;
using t.Infrastructure.Email;
using t.Infrastructure.Localization;
using t.Models.Entities;

namespace t.Infrastructure.Maintenance;

/// <summary>
/// Sau khi yêu cầu sự cố được lưu: chọn thợ theo loại sự cố, gán, rồi gửi cho mỗi vai trò đúng loại thư của mình.
/// <list type="bullet">
/// <item>Thợ: thư giao việc khi được giao.</item>
/// <item>Quản lý: thư khi cần ra tay (không có thợ phù hợp, mô tả mơ hồ) hoặc cần theo dõi (khẩn cấp).</item>
/// <item>Quản trị viên: thư về hệ thống (mô hình AI không phản hồi), và nhận thay khi chưa có quản lý nào.</item>
/// </list>
/// Mọi lỗi chỉ ghi log: yêu cầu của cư dân đã lưu, không được hỏng vì email.
/// </summary>
public class MaintenanceDispatcher
{
    private static readonly MaintenanceStatus[] ActiveStatuses =
        { MaintenanceStatus.Open, MaintenanceStatus.Acknowledged, MaintenanceStatus.InProgress };

    // ponytail: mô hình chỉ có một loại "structural" nên tách thợ mộc / nhôm kính bằng từ khóa.
    // Sai khi mô tả không dấu dùng "kinh" theo nghĩa khác ("kinh khung"); muốn chắc thì thêm
    // loại riêng vào prompt và train lại.
    private static readonly string[] GlassWords = { "kinh", "nhom", "cua-so", "cua-lua" };

    // ponytail: chặn bằng biến tĩnh để Ollama tắt lâu không sinh một thư cho mỗi sự cố.
    // Mất khi app khởi động lại; cần bền hơn thì lưu thời điểm vào DB.
    private static readonly TimeSpan AiAlertInterval = TimeSpan.FromMinutes(30);
    private static DateTime _lastAiAlertUtc = DateTime.MinValue;

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

    /// <param name="aiUnavailable">Mô hình được bật nhưng không trả kết quả dùng được cho yêu cầu này.</param>
    public async Task DispatchAsync(MaintenanceRequest req, MaintenanceTriage? triage,
        bool aiUnavailable = false, CancellationToken ct = default)
    {
        try
        {
            // Mô tả mơ hồ thì loại do mô hình đoán không đáng tin: để quản lý hỏi lại trước khi giao thợ.
            var isVague = triage?.FollowUpQuestion != null;
            var role = isVague ? null : RoleFor(req.Category, req.Description);
            var assignee = role == null ? null : await LeastBusyAsync(role, ct);
            if (assignee != null)
            {
                req.AssignedToId = assignee.Id;
                await _db.SaveChangesAsync(ct);
            }

            // Đọc DB xong hết rồi mới gửi: DbContext không dùng song song được.
            await _db.Entry(req).Reference(r => r.Apartment).LoadAsync(ct);
            await _db.Entry(req).Reference(r => r.Reporter).LoadAsync(ct);
            var admins = await UsersInRoleAsync(SeedData.RoleAdmin, ct);
            var managers = await UsersInRoleAsync(SeedData.RoleManager, ct);
            if (managers.Count == 0) managers = admins;

            var isUrgent = req.Priority == MaintenancePriority.Urgent;
            var tag = isUrgent ? "KHẨN CẤP - " : "";
            var label = $"{req.RequestNumber}: {req.Title.ReplaceLineEndings(" ")}";
            var details = BuildDetails(req, triage);
            var sends = new List<Task>();

            if (assignee != null)
                sends.Add(SendSafeAsync(new[] { assignee }, $"[Luxe Haven] {tag}Giao việc {label}",
                    "<p>Anh/chị được giao xử lý sự cố sau.</p>" + details, req.RequestNumber, ct));

            var managerMail = ManagerMail(assignee, isVague, isUrgent, triage);
            if (managerMail != null)
                sends.Add(SendSafeAsync(managers, $"[Luxe Haven] {tag}{managerMail.Value.Subject} {label}",
                    managerMail.Value.Lead + details, req.RequestNumber, ct));

            if (aiUnavailable && DateTime.UtcNow - _lastAiAlertUtc >= AiAlertInterval)
            {
                _lastAiAlertUtc = DateTime.UtcNow;
                sends.Add(SendSafeAsync(admins, "[Luxe Haven] Mô hình phân loại sự cố không phản hồi",
                    $"<p>Yêu cầu <strong>{E(req.RequestNumber)}</strong> được lưu theo lựa chọn của cư dân vì mô hình AI không trả kết quả.</p>" +
                    "<p>Cần kiểm tra Ollama có đang chạy và mô hình trong cấu hình <code>Ollama:Model</code> đã được nạp.</p>" +
                    $"<p>Trong {AiAlertInterval.TotalMinutes:0} phút tới sẽ không gửi thêm thư này.</p>",
                    req.RequestNumber, ct));
            }

            // Gửi song song: mỗi lần kết nối SMTP mất vài giây, gửi lần lượt làm cư dân chờ lâu.
            await Task.WhenAll(sends);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Maintenance dispatch failed for {RequestNumber}", req.RequestNumber);
        }
    }

    /// <summary>Thư cho quản lý, hoặc null khi sự cố thường đã giao được thợ (quản lý xem trên trang Bảo trì).</summary>
    private static (string Subject, string Lead)? ManagerMail(AppUser? assignee, bool isVague, bool isUrgent, MaintenanceTriage? triage)
    {
        if (assignee != null)
            return isUrgent
                ? ("Cần theo dõi", $"<p>Sự cố khẩn cấp, đã giao cho <strong>{E(assignee.FullName)}</strong>. Cần quản lý theo dõi.</p>")
                : null;
        return isVague
            ? ("Cần hỏi lại cư dân", $"<p>Mô tả chưa đủ rõ nên chưa giao thợ. Cần hỏi lại cư dân: <em>{E(triage!.FollowUpQuestion)}</em></p>")
            : ("Cần phân công", "<p>Chưa có thợ chuyên trách nhận sự cố này. Cần quản lý phân công.</p>");
    }

    /// <summary>Một thư cho cả nhóm người nhận (không phải mỗi người một thư).</summary>
    private async Task SendSafeAsync(IEnumerable<AppUser> to, string subject, string html, string requestNumber, CancellationToken ct)
    {
        var addresses = string.Join(", ", to.Select(u => u.Email).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct());
        if (addresses.Length == 0) return;
        try { await _email.SendAsync(addresses, subject, html, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "Could not email {To} about {RequestNumber}", addresses, requestNumber); }
    }

    private static bool IsGlassWork(string description)
    {
        var slug = "-" + Slugify.Make(description) + "-";
        return GlassWords.Any(w => slug.Contains("-" + w + "-"));
    }

    private async Task<AppUser?> LeastBusyAsync(string role, CancellationToken ct)
    {
        var staff = await UsersInRoleAsync(role, ct);
        if (staff.Count == 0) return null;
        var ids = staff.Select(s => s.Id).ToList();
        var load = await _db.MaintenanceRequests
            .Where(m => m.AssignedToId != null && ids.Contains(m.AssignedToId) && ActiveStatuses.Contains(m.Status))
            .GroupBy(m => m.AssignedToId!)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        return staff.OrderBy(s => load.GetValueOrDefault(s.Id)).ThenBy(s => s.Id).First();
    }

    private Task<List<AppUser>> UsersInRoleAsync(string role, CancellationToken ct) =>
        (from u in _db.Users
         join ur in _db.UserRoles on u.Id equals ur.UserId
         join r in _db.Roles on ur.RoleId equals r.Id
         where r.Name == role
         orderby u.Email
         select u).ToListAsync(ct);

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    private static string BuildDetails(MaintenanceRequest req, MaintenanceTriage? triage)
    {
        var reporter = req.Reporter;
        var phone = reporter?.Phone ?? reporter?.PhoneNumber;
        var danger = triage?.IsDangerous == true
            ? "<p><strong>Cảnh báo: sự cố có nguy cơ gây mất an toàn.</strong></p>" : "";
        return $@"
<p>Yêu cầu <strong>{E(req.RequestNumber)}</strong> · {E(req.Category.Vi())} · Ưu tiên: <strong>{E(req.Priority.Vi())}</strong></p>
{danger}
<p>Căn hộ: <strong>{E(req.Apartment?.Title)}</strong><br/>
Người báo: {E(reporter?.FullName)} · {E(phone)}</p>
<p><strong>{E(req.Title)}</strong></p>
<p style=""white-space:pre-wrap"">{E(req.Description)}</p>
<p>— Luxe Haven</p>";
    }
}
