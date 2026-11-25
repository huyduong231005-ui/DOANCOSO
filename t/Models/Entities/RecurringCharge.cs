namespace t.Models.Entities;

/// <summary>
/// Căn đăng ký dùng một dịch vụ cố định hằng tháng (gửi xe máy, gửi ô tô) với số lượng.
/// Tên và đơn giá lấy từ loại dịch vụ, không lưu lại ở đây. Huỷ đăng ký thì xoá dòng.
/// </summary>
public class RecurringCharge
{
    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public int UtilityTypeId { get; set; }
    public UtilityType UtilityType { get; set; } = null!;

    public int Quantity { get; set; } = 1;
}
