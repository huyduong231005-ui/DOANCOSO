using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum MaintenanceCategory
{
    Plumbing = 0,
    Electrical = 1,
    Appliance = 2,
    Structural = 3,
    Cleaning = 4,
    Pest = 5,
    Network = 6,
    Other = 99
}

public enum MaintenancePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

// Giá trị số lưu trong DB: chỉ thêm ở cuối, không đổi số của trạng thái cũ.
public enum MaintenanceStatus
{
    Open = 0,
    Acknowledged = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4,
    Rejected = 5,
    OnHold = 6,     // chờ vật tư / nhà thầu / lịch hẹn: không tính vào hạn SLA
    Cancelled = 7   // cư dân huỷ trước khi kỹ thuật bắt đầu
}

public enum MaintenancePhotoStage
{
    Report = 0, // ảnh hiện trạng lúc báo
    Result = 1  // ảnh nghiệm thu sau khi sửa
}

public class MaintenanceRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;

    /// <summary>
    /// Phiên bản dòng để chặn hai người cùng sửa một yêu cầu (khoá lạc quan): ánh xạ vào cột hệ thống xmin
    /// của PostgreSQL, tự đổi mỗi lần dòng được cập nhật, không phải cột của bảng. (Cột PhienBan kiểu byte[]
    /// của BaseEntity không được PostgreSQL tự sinh nên không dùng được cho việc này.)
    /// </summary>
    public uint Xmin { get; set; }

    // Sự cố nằm ở đúng một nơi: trong một căn (ApartmentId) HOẶC khu vực chung của một toà
    // (CommonBuildingId + Location). Không lưu toà cho sự cố trong căn vì toà suy ra từ căn (3NF).
    public int? ApartmentId { get; set; }
    public Apartment? Apartment { get; set; }

    public int? CommonBuildingId { get; set; }
    public Building? CommonBuilding { get; set; }
    public string? Location { get; set; }

    public string? ReporterId { get; set; }
    public AppUser? Reporter { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public MaintenanceCategory Category { get; set; } = MaintenanceCategory.Other;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;

    public string? AssignedToId { get; set; }
    public AppUser? AssignedTo { get; set; }

    public string? ResolutionNote { get; set; }

    /// <summary>
    /// JSON nguyên văn mô hình AI trả về lúc cư dân gửi yêu cầu (null nếu mô hình không chạy).
    /// So với Category/Priority sau khi admin sửa để đo độ chính xác và gom dữ liệu train.
    /// </summary>
    public string? AiTriageJson { get; set; }

    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public bool ChargeToTenant { get; set; }

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    // Mốc tiếp nhận, giải quyết, đóng không lưu thành cột: lấy từ lịch sử trạng thái (cần nạp History),
    // để một thông tin chỉ nằm ở một bảng. Câu truy vấn dùng các biểu thức tương ứng ở MaintenanceMilestones.
    public DateTime? AcknowledgedAt => History.Where(h => h.ToStatus == MaintenanceStatus.Acknowledged)
        .Select(h => (DateTime?)h.ChangedAt).Min();
    public DateTime? ResolvedAt => Status is MaintenanceStatus.Resolved or MaintenanceStatus.Closed
        ? History.Where(h => h.ToStatus == MaintenanceStatus.Resolved).Select(h => (DateTime?)h.ChangedAt).Max() : null;
    public DateTime? ClosedAt => Status is MaintenanceStatus.Closed or MaintenanceStatus.Rejected or MaintenanceStatus.Cancelled
        ? History.Where(h => h.ToStatus == Status).Select(h => (DateTime?)h.ChangedAt).Max() : null;

    /// <summary>
    /// Hạn hoàn tất theo SLA, chốt lúc báo (NgayBaoSuCo + thời hạn của mức ưu tiên), cộng thêm thời gian tạm dừng.
    /// Lưu lại chứ không tính lại từ bảng MucSla vì SLA có thể đổi về sau, hạn đã cam kết với cư dân thì không.
    /// </summary>
    public DateTime? DueAt { get; set; }

    /// <summary>Cư dân đánh giá 1–5 khi xác nhận đã xử lý xong.</summary>
    public int? Rating { get; set; }
    public string? RatingComment { get; set; }

    public List<MaintenancePhoto> Photos { get; set; } = new();
    public List<MaintenanceStatusChange> History { get; set; } = new();

    /// <summary>"Căn A-12.05" hoặc "Khu vực chung: Sảnh tầng 1" (cần nạp Apartment).</summary>
    public string PlaceLabel => Apartment != null ? "Căn " + Apartment.UnitCode : "Khu vực chung: " + Location;

    public static readonly MaintenanceStatus[] ActiveStatuses =
        { MaintenanceStatus.Open, MaintenanceStatus.Acknowledged, MaintenanceStatus.InProgress, MaintenanceStatus.OnHold };
}

public class MaintenancePhoto
{
    public int Id { get; set; }
    public int MaintenanceRequestId { get; set; }
    public MaintenanceRequest MaintenanceRequest { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public MaintenancePhotoStage Stage { get; set; }
    public string? UploadedById { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Một lần đổi trạng thái (hoặc giao việc). FromStatus null = lúc tạo yêu cầu.</summary>
public class MaintenanceStatusChange
{
    public int Id { get; set; }
    public int MaintenanceRequestId { get; set; }
    public MaintenanceRequest MaintenanceRequest { get; set; } = null!;
    public MaintenanceStatus? FromStatus { get; set; }
    public MaintenanceStatus ToStatus { get; set; }
    public string? ChangedById { get; set; } // null = hệ thống (AI điều phối)
    public AppUser? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}

/// <summary>Thời hạn theo mức ưu tiên. Giá trị seed là giả định của nhóm, quản trị chỉnh trong DB.</summary>
/// <summary>Bảng MucUuTien: danh mục mức ưu tiên của sự cố và thời hạn SLA của từng mức.</summary>
public class SlaPolicy
{
    public MaintenancePriority Priority { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ResponseMinutes { get; set; }
    public int ResolveMinutes { get; set; }

    // Giả định của nhóm (chưa xác minh tại đơn vị thực tế): khẩn cấp 15 phút / 2 giờ, cao 30 phút / 8 giờ,
    // trung bình 2 giờ / 24 giờ, thấp 12 giờ / 3 ngày.
    public static readonly SlaPolicy[] Defaults =
    {
        new() { Priority = MaintenancePriority.Urgent, Name = "Khẩn cấp", ResponseMinutes = 15, ResolveMinutes = 120 },
        new() { Priority = MaintenancePriority.High, Name = "Cao", ResponseMinutes = 30, ResolveMinutes = 480 },
        new() { Priority = MaintenancePriority.Medium, Name = "Trung bình", ResponseMinutes = 120, ResolveMinutes = 1440 },
        new() { Priority = MaintenancePriority.Low, Name = "Thấp", ResponseMinutes = 720, ResolveMinutes = 4320 },
    };
}
