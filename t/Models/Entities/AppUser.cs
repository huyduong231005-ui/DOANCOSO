using Microsoft.AspNetCore.Identity;
using t.Models.Entities.Common;

namespace t.Models.Entities;

public class AppUser : IdentityUser, IAuditable, ISoftDeletable
{
    public string FullName { get; set; } = string.Empty;
    // Số điện thoại dùng PhoneNumber của Identity (cột SoDienThoai), không có cột thứ hai.
    /// <summary>Số giấy tờ tuỳ thân khai báo cư trú (thông tin của người, nên nằm ở NguoiDung).</summary>
    public string? IdNumber { get; set; }

    /// <summary>Tài khoản do Ban quản lý tạo sẵn khi thêm cư dân, chưa đặt mật khẩu qua thư mời.</summary>
    public bool IsActivated => PasswordHash != null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }


    public ICollection<ApartmentResident> Residencies { get; set; } = new List<ApartmentResident>();
}
