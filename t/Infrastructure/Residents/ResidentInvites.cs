using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Email;
using t.Models.Entities;

namespace t.Infrastructure.Residents;

/// <summary>
/// Thư mời tạo tài khoản cổng cư dân cho người Ban quản lý đã thêm vào căn khi họ chưa có tài khoản.
/// Liên kết theo mã mời trong thư chứ không theo email trùng: trang đăng ký không xác minh email,
/// nên chỉ người mở được hộp thư mới gắn được tài khoản vào căn.
/// Mã ký bằng Data Protection (không cần bảng lưu), hết hạn sau <see cref="Lifetime"/>.
/// </summary>
public class ResidentInvites
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(14);

    private readonly ITimeLimitedDataProtector _protector;
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<ResidentInvites> _log;

    public ResidentInvites(IDataProtectionProvider protection, AppDbContext db, IEmailSender email, ILogger<ResidentInvites> log)
    {
        _protector = protection.CreateProtector("ResidentInvite.v1").ToTimeLimitedDataProtector();
        _db = db;
        _email = email;
        _log = log;
    }

    public string CreateToken(ApartmentResident r) => _protector.Protect($"{r.Id}|{r.Email}", Lifetime);

    /// <summary>Bản ghi cư dân của lời mời, hoặc null nếu mã sai, hết hạn, cư dân đã chuyển đi hay đã đổi email.</summary>
    public async Task<ApartmentResident?> FindAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        string payload;
        try { payload = _protector.Unprotect(token); }
        catch (CryptographicException) { return null; }

        var parts = payload.Split('|', 2);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var id)) return null;
        var r = await _db.ApartmentResidents.Include(x => x.Apartment)
            .FirstOrDefaultAsync(x => x.Id == id && x.MoveOutDate == null, ct);
        return r != null && string.Equals(r.Email, parts[1], StringComparison.OrdinalIgnoreCase) ? r : null;
    }

    /// <summary>Gắn tài khoản (đúng email được mời) vào căn. Trả về bản ghi cư dân, null nếu lời mời không dùng được.</summary>
    public async Task<ApartmentResident?> AcceptAsync(string? token, AppUser user, CancellationToken ct = default)
    {
        var r = await FindAsync(token, ct);
        if (r == null || !string.Equals(user.Email, r.Email, StringComparison.OrdinalIgnoreCase)) return null;
        if (r.UserId != null && r.UserId != user.Id) return null;
        r.UserId = user.Id;
        await _db.SaveChangesAsync(ct);
        return r;
    }

    /// <summary>Gửi thư mời; lỗi gửi thư chỉ ghi log để không chặn việc thêm cư dân.</summary>
    public async Task<bool> SendAsync(ApartmentResident r, string unitCode, string inviteUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(r.Email)) return false;
        var html =
            $"<p>Chào {WebUtility.HtmlEncode(r.FullName)},</p>" +
            $"<p>Ban quản lý đã ghi nhận bạn là cư dân căn <strong>{WebUtility.HtmlEncode(unitCode)}</strong>.</p>" +
            "<p>Tạo tài khoản cổng cư dân để xem hoá đơn, thanh toán phí và báo sự cố trực tuyến:</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(inviteUrl)}\" style=\"display:inline-block;padding:10px 18px;background:#0f2a44;color:#fff;border-radius:8px;text-decoration:none\">Tạo tài khoản</a></p>" +
            $"<p style=\"color:#64748b;font-size:13px\">Liên kết có hiệu lực {Lifetime.Days} ngày. Nếu bạn đã có tài khoản với email này, bấm liên kết để gắn căn vào tài khoản.</p>";
        try
        {
            await _email.SendAsync(r.Email, $"Mời tạo tài khoản cư dân căn {unitCode}", html, ct);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not send resident invite to {Email}", r.Email);
            return false;
        }
    }
}
