using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum ResidentRole
{
    Owner = 0,
    Tenant = 1,
    Member = 2
}

/// <summary>
/// Một lần cư trú (bảng CuTru): người dùng nào ở căn nào, với vai trò gì, từ ngày nào đến ngày nào.
/// Người và tài khoản là một (NguoiDung); họ tên, số điện thoại, email chỉ lưu ở NguoiDung.
/// Ban quản lý thêm người chưa dùng cổng cư dân thì hệ thống tạo sẵn tài khoản chưa đặt mật khẩu,
/// người đó kích hoạt qua thư mời. Đây là liên kết thường, không phải hợp thành: căn còn người cư trú
/// thì không xoá được, và xoá căn không xoá người.
/// IsPrimary đánh dấu người đại diện căn (nhận thông báo phí, email nhắc nợ).
/// </summary>
public class ApartmentResident : BaseEntity
{
    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public ResidentRole Role { get; set; } = ResidentRole.Owner;
    public bool IsPrimary { get; set; }

    public DateTime MoveInDate { get; set; } = DateTime.UtcNow;
    public DateTime? MoveOutDate { get; set; }

    /// <summary>
    /// Lúc người cư trú bấm thư mời xác nhận (kích hoạt tài khoản mới, hoặc nhận căn vào tài khoản đã có).
    /// Chưa xác nhận thì cổng cư dân chưa hiện căn này: BQL gõ nhầm email cũng không làm lộ dữ liệu căn (BR-CD-02).
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }


    public bool IsActive => MoveOutDate == null;
}
