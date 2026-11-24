using t.Models.Entities;

namespace t.Infrastructure.Residents;

/// <summary>
/// Tài khoản tạo sẵn cho người Ban quản lý ghi nhận cư trú khi họ chưa dùng cổng cư dân.
/// Chưa có mật khẩu nên chưa đăng nhập được; người đó đặt mật khẩu qua thư mời (BR-CD-02).
/// </summary>
public static class PendingAccounts
{
    public static AppUser New(string fullName, string? phone, string? email, string? idNumber)
    {
        // Không có email thì tên đăng nhập là mã ngẫu nhiên; người đó chỉ kích hoạt được khi BQL bổ sung email.
        var userName = string.IsNullOrWhiteSpace(email) ? "cudan-" + Guid.NewGuid().ToString("N")[..12] : email.Trim();
        return new AppUser
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            NormalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToUpperInvariant(),
            FullName = fullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            IdNumber = string.IsNullOrWhiteSpace(idNumber) ? null : idNumber.Trim(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            LockoutEnabled = true
        };
    }
}
