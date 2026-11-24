using System.Net;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Email;
using t.Infrastructure.Localization;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Infrastructure.Maintenance;

/// <summary>
/// Cảnh báo quá hạn SLA: định kỳ tìm yêu cầu còn mở đã qua hạn hoàn tất mà chưa được báo, gửi một thư tổng hợp
/// cho quản lý (không có quản lý thì gửi quản trị) và ghi một dòng lịch sử làm dấu "đã báo".
/// </summary>
public class MaintenanceOverdueAlertService : BackgroundService
{
    /// <summary>Ghi chú của dòng lịch sử đánh dấu đã cảnh báo (người thực hiện để trống = hệ thống).</summary>
    public const string AlertNote = "Quá hạn SLA, hệ thống đã báo quản lý";

    // ponytail: mỗi yêu cầu chỉ báo một lần; nếu hạn được dời (tạm dừng) rồi lại quá hạn thì không báo lần hai.
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceProvider _services;
    private readonly ILogger<MaintenanceOverdueAlertService> _log;

    public MaintenanceOverdueAlertService(IServiceProvider services, ILogger<MaintenanceOverdueAlertService> log)
    {
        _services = services;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Lượt đầu chạy sau một phút để ứng dụng khởi động xong (và không chen vào kiểm thử tích hợp).
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var count = await RunOnceAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                    scope.ServiceProvider.GetRequiredService<IEmailSender>(), DateTime.UtcNow, stoppingToken);
                if (count > 0) _log.LogInformation("Overdue alert sent for {Count} maintenance requests", count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Overdue alert tick failed");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>Một lượt quét. Trả số yêu cầu vừa được cảnh báo; thư lỗi thì không đánh dấu để lượt sau gửi lại.</summary>
    public static async Task<int> RunOnceAsync(AppDbContext db, IEmailSender email, DateTime nowUtc, CancellationToken ct = default)
    {
        var due = await db.MaintenanceRequests
            .Where(MaintenanceWorkflow.IsOverdue(nowUtc))
            .Where(r => !r.History.Any(h => h.Note == AlertNote))
            .OrderBy(r => r.DueAt)
            .Select(r => new
            {
                r.Id, r.RequestNumber, r.Title, r.Status, r.Priority, r.DueAt,
                Place = r.Apartment != null ? "Căn " + r.Apartment.UnitCode : "Khu vực chung: " + r.Location,
                Assignee = r.AssignedTo != null ? r.AssignedTo.FullName : null
            })
            .ToListAsync(ct);
        if (due.Count == 0) return 0;

        var to = await RecipientsAsync(db, SeedData.RoleManager, ct);
        if (to.Count == 0) to = await RecipientsAsync(db, SeedData.RoleAdmin, ct);
        if (to.Count == 0) return 0;

        static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        var rows = string.Concat(due.Select(r =>
            $"<tr><td>{E(r.RequestNumber)}</td><td>{E(r.Title)}</td><td>{E(r.Place)}</td>" +
            $"<td>{E(r.Priority.Vi())}</td><td>{E(r.Status.Vi())}</td><td>{E(r.Assignee ?? "Chưa phân công")}</td>" +
            $"<td>{VnTime.FromUtc(r.DueAt!.Value):HH:mm dd/MM/yyyy}</td></tr>"));
        var html =
            $"<p>Có {due.Count} yêu cầu sự cố đã quá hạn hoàn tất theo SLA:</p>" +
            "<table border=\"1\" cellpadding=\"4\" cellspacing=\"0\"><tr><th>Yêu cầu</th><th>Tiêu đề</th><th>Nơi</th>" +
            "<th>Mức</th><th>Trạng thái</th><th>Phụ trách</th><th>Hạn</th></tr>" + rows + "</table>" +
            "<p>Xem tại mục Sự cố &amp; bảo trì, bộ lọc Quá hạn. Mỗi yêu cầu chỉ được nhắc một lần.</p>";

        await email.SendAsync(string.Join(", ", to), $"[Luxe Haven] {due.Count} yêu cầu sự cố quá hạn SLA", html, ct);

        foreach (var r in due)
            db.MaintenanceStatusChanges.Add(new MaintenanceStatusChange
            {
                MaintenanceRequestId = r.Id, FromStatus = r.Status, ToStatus = r.Status,
                ChangedById = null, ChangedAt = nowUtc, Note = AlertNote
            });
        await db.SaveChangesAsync(ct);
        return due.Count;
    }

    private static Task<List<string>> RecipientsAsync(AppDbContext db, string role, CancellationToken ct) =>
        (from u in db.Users
         join ur in db.UserRoles on u.Id equals ur.UserId
         join r in db.Roles on ur.RoleId equals r.Id
         where r.Name == role && u.Email != null && (u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow)
         select u.Email!).Distinct().ToListAsync(ct);
}
