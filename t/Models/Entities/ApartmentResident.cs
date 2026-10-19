using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum ResidentRole
{
    Owner = 0,
    Tenant = 1,
    Member = 2
}

/// <summary>
/// Người đang (hoặc đã) cư trú tại căn hộ — chủ sở hữu, người thuê lại hoặc thành viên hộ.
/// Ban quản lý ghi nhận cả người chưa có tài khoản; UserId chỉ có khi cư dân dùng cổng cư dân.
/// IsPrimary đánh dấu người đại diện căn (nhận thông báo phí, email nhắc nợ).
/// </summary>
public class ApartmentResident : BaseEntity
{
    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? IdNumber { get; set; }

    public ResidentRole Role { get; set; } = ResidentRole.Owner;
    public bool IsPrimary { get; set; }

    public DateTime MoveInDate { get; set; } = DateTime.UtcNow;
    public DateTime? MoveOutDate { get; set; }

    public string? UserId { get; set; }
    public AppUser? User { get; set; }

    public string? Note { get; set; }

    public bool IsActive => MoveOutDate == null;
}
