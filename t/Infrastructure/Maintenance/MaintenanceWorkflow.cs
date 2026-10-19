using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Localization;
using t.Models.Entities;

namespace t.Infrastructure.Maintenance;

/// <summary>Ai đang thao tác trên yêu cầu sự cố. System = AI điều phối / tác vụ nền.</summary>
public enum WorkflowActor
{
    Resident,
    Technician,
    Manager,
    System
}

/// <summary>
/// Nơi duy nhất đổi trạng thái yêu cầu sự cố. Mọi màn hình (quản lý, kỹ thuật viên, cư dân, AI điều phối)
/// đều đi qua đây nên quy tắc nghiệp vụ, mốc thời gian, hạn SLA và lịch sử luôn nhất quán.
/// </summary>
public class MaintenanceWorkflow
{
    // Bảng chuyển trạng thái (BR-SC-03): (từ, sang) → ai được làm.
    private static readonly Dictionary<(MaintenanceStatus From, MaintenanceStatus To), WorkflowActor[]> Rules = new()
    {
        [(MaintenanceStatus.Open, MaintenanceStatus.Acknowledged)] = new[] { WorkflowActor.Manager, WorkflowActor.System },
        [(MaintenanceStatus.Open, MaintenanceStatus.Rejected)] = new[] { WorkflowActor.Manager },
        [(MaintenanceStatus.Open, MaintenanceStatus.Cancelled)] = new[] { WorkflowActor.Resident, WorkflowActor.Manager },
        [(MaintenanceStatus.Acknowledged, MaintenanceStatus.InProgress)] = new[] { WorkflowActor.Technician, WorkflowActor.Manager },
        [(MaintenanceStatus.Acknowledged, MaintenanceStatus.Rejected)] = new[] { WorkflowActor.Manager },
        [(MaintenanceStatus.Acknowledged, MaintenanceStatus.Cancelled)] = new[] { WorkflowActor.Resident, WorkflowActor.Manager },
        [(MaintenanceStatus.InProgress, MaintenanceStatus.OnHold)] = new[] { WorkflowActor.Technician, WorkflowActor.Manager },
        [(MaintenanceStatus.InProgress, MaintenanceStatus.Resolved)] = new[] { WorkflowActor.Technician, WorkflowActor.Manager },
        [(MaintenanceStatus.OnHold, MaintenanceStatus.InProgress)] = new[] { WorkflowActor.Technician, WorkflowActor.Manager },
        [(MaintenanceStatus.Resolved, MaintenanceStatus.Closed)] = new[] { WorkflowActor.Resident, WorkflowActor.Manager },
        [(MaintenanceStatus.Resolved, MaintenanceStatus.InProgress)] = new[] { WorkflowActor.Resident, WorkflowActor.Manager },
    };

    // Chuyển trạng thái nào bắt buộc ghi lý do / kết quả.
    private static readonly HashSet<(MaintenanceStatus, MaintenanceStatus)> NoteRequired = new()
    {
        (MaintenanceStatus.Open, MaintenanceStatus.Rejected),
        (MaintenanceStatus.Acknowledged, MaintenanceStatus.Rejected),
        (MaintenanceStatus.InProgress, MaintenanceStatus.OnHold),
        (MaintenanceStatus.Resolved, MaintenanceStatus.InProgress),
    };

    private readonly AppDbContext _db;

    public MaintenanceWorkflow(AppDbContext db) => _db = db;

    /// <summary>Các trạng thái kế tiếp mà người này được chuyển sang (để hiện nút trên giao diện).</summary>
    public static IReadOnlyList<MaintenanceStatus> NextStatuses(MaintenanceRequest r, WorkflowActor actor, string? userId) =>
        Rules.Where(kv => kv.Key.From == r.Status && Check(r, kv.Key.To, actor, userId, "x", "x") == null)
             .Select(kv => kv.Key.To).ToList();

    /// <summary>Lý do không được chuyển, hoặc null nếu hợp lệ. Không đụng DB nên dùng được trong unit test.</summary>
    public static string? Check(MaintenanceRequest r, MaintenanceStatus to, WorkflowActor actor, string? userId,
        string? note, string? resolutionNote)
    {
        if (!Rules.TryGetValue((r.Status, to), out var allowed))
            return $"Không thể chuyển từ \"{r.Status.Vi()}\" sang \"{to.Vi()}\".";
        if (!allowed.Contains(actor))
            return $"Bạn không có quyền chuyển yêu cầu sang \"{to.Vi()}\".";
        // BR-SC-04: kỹ thuật viên chỉ thao tác trên việc được giao cho mình.
        if (actor == WorkflowActor.Technician && (userId == null || r.AssignedToId != userId))
            return "Yêu cầu này không được giao cho bạn.";
        if (to == MaintenanceStatus.InProgress && r.AssignedToId == null)
            return "Phải giao cho kỹ thuật viên trước khi bắt đầu xử lý.";
        // BR-SC-06: hoàn tất phải có kết quả xử lý.
        if (to == MaintenanceStatus.Resolved && string.IsNullOrWhiteSpace(resolutionNote ?? r.ResolutionNote))
            return "Phải ghi kết quả xử lý trước khi hoàn tất.";
        if (NoteRequired.Contains((r.Status, to)) && string.IsNullOrWhiteSpace(note))
            return "Phải ghi lý do.";
        return null;
    }

    /// <summary>
    /// Đổi trạng thái: kiểm tra quy tắc, cập nhật mốc thời gian và hạn SLA, ghi lịch sử, lưu DB.
    /// Trả về lỗi nghiệp vụ (null = thành công).
    /// </summary>
    public async Task<string?> TransitionAsync(MaintenanceRequest r, MaintenanceStatus to, WorkflowActor actor,
        string? userId, string? note = null, string? resolutionNote = null, CancellationToken ct = default)
    {
        var error = Check(r, to, actor, userId, note, resolutionNote);
        if (error != null) return error;

        var now = DateTime.UtcNow;
        var from = r.Status;

        // Tạm dừng không tính vào hạn: khi chạy lại thì dời hạn thêm đúng khoảng đã dừng.
        if (from == MaintenanceStatus.OnHold && r.DueAt != null)
        {
            var pausedAt = await _db.MaintenanceStatusChanges
                .Where(h => h.MaintenanceRequestId == r.Id && h.ToStatus == MaintenanceStatus.OnHold)
                .MaxAsync(h => (DateTime?)h.ChangedAt, ct);
            if (pausedAt != null) r.DueAt = r.DueAt.Value + (now - pausedAt.Value);
        }

        switch (to)
        {
            case MaintenanceStatus.Acknowledged:
                r.AcknowledgedAt ??= now;
                break;
            case MaintenanceStatus.InProgress:
                r.AcknowledgedAt ??= now;
                if (from == MaintenanceStatus.Resolved) r.ResolvedAt = null; // cư dân báo chưa khắc phục
                break;
            case MaintenanceStatus.Resolved:
                if (!string.IsNullOrWhiteSpace(resolutionNote)) r.ResolutionNote = resolutionNote.Trim();
                r.ResolvedAt = now;
                break;
            case MaintenanceStatus.Closed:
                r.ClosedAt = now;
                break;
            case MaintenanceStatus.Rejected:
                r.ResolutionNote = "Từ chối: " + note!.Trim();
                r.ClosedAt = now;
                break;
            case MaintenanceStatus.Cancelled:
                r.ClosedAt = now;
                break;
        }

        r.Status = to;
        AddHistory(r, from, to, userId, note);
        await _db.SaveChangesAsync(ct);
        return null;
    }

    /// <summary>
    /// Giao (hoặc giao lại) cho kỹ thuật viên. Yêu cầu mới gửi chuyển sang "Đã tiếp nhận".
    /// AI điều phối (System) chỉ được giao yêu cầu còn ở trạng thái mới gửi.
    /// </summary>
    public async Task<string?> AssignAsync(MaintenanceRequest r, AppUser assignee, WorkflowActor actor, string? userId,
        string? note = null, CancellationToken ct = default)
    {
        if (actor is not (WorkflowActor.Manager or WorkflowActor.System))
            return "Chỉ quản lý mới được giao việc.";
        if (!MaintenanceRequest.ActiveStatuses.Contains(r.Status))
            return $"Không thể giao việc khi yêu cầu đã {r.Status.Vi()}.";
        if (actor == WorkflowActor.System && r.Status != MaintenanceStatus.Open)
            return "Hệ thống chỉ tự giao yêu cầu mới gửi.";

        r.AssignedToId = assignee.Id;
        var text = $"Giao cho {assignee.FullName}" + (string.IsNullOrWhiteSpace(note) ? "" : ": " + note.Trim());
        if (r.Status == MaintenanceStatus.Open)
            return await TransitionAsync(r, MaintenanceStatus.Acknowledged, actor, userId, text, ct: ct);

        AddHistory(r, r.Status, r.Status, userId, text);
        await _db.SaveChangesAsync(ct);
        return null;
    }

    /// <summary>Gắn hạn SLA và ghi lịch sử "tạo yêu cầu". Gọi trước lần SaveChanges đầu tiên của yêu cầu mới.</summary>
    public async Task InitializeNewAsync(MaintenanceRequest r, string? userId, CancellationToken ct = default)
    {
        await RecalculateDueAsync(r, ct);
        r.History.Add(new MaintenanceStatusChange
        {
            FromStatus = null, ToStatus = r.Status, ChangedById = userId, ChangedAt = r.ReportedAt, Note = "Tạo yêu cầu"
        });
    }

    /// <summary>Hạn hoàn tất = lúc báo + thời hạn của mức ưu tiên. Gọi lại khi quản lý đổi mức ưu tiên.</summary>
    public async Task RecalculateDueAsync(MaintenanceRequest r, CancellationToken ct = default)
    {
        var sla = await _db.SlaPolicies.FindAsync(new object[] { r.Priority }, ct);
        r.DueAt = sla == null ? null : r.ReportedAt.AddMinutes(sla.ResolveMinutes);
    }

    /// <summary>Ô tải ảnh trên form trả về danh sách URL cách nhau dấu phẩy; mỗi URL thành một dòng AnhSuCo.</summary>
    public static IEnumerable<MaintenancePhoto> PhotosFrom(string? urls, MaintenancePhotoStage stage, string? userId) =>
        (urls ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(u => u.Length <= 500)
            .Distinct()
            .Select(u => new MaintenancePhoto { Url = u, Stage = stage, UploadedById = userId });

    /// <summary>Đang mở và đã quá hạn hoàn tất (dùng trong truy vấn).</summary>
    public static System.Linq.Expressions.Expression<Func<MaintenanceRequest, bool>> IsOverdue(DateTime now) =>
        r => r.DueAt != null && r.DueAt < now &&
             (r.Status == MaintenanceStatus.Open || r.Status == MaintenanceStatus.Acknowledged ||
              r.Status == MaintenanceStatus.InProgress || r.Status == MaintenanceStatus.OnHold);

    private void AddHistory(MaintenanceRequest r, MaintenanceStatus from, MaintenanceStatus to, string? userId, string? note) =>
        _db.MaintenanceStatusChanges.Add(new MaintenanceStatusChange
        {
            MaintenanceRequestId = r.Id, MaintenanceRequest = r, FromStatus = from, ToStatus = to,
            ChangedById = userId, ChangedAt = DateTime.UtcNow, Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });
}
