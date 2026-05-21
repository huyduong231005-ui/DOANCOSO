using System.ComponentModel.DataAnnotations;

namespace t.Models.ViewModels;

public class BookViewingViewModel
{
    public int ApartmentId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 100 ký tự.")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^[0-9+()\s\-]{8,20}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string ContactPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(200)]
    public string? ContactEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày xem.")]
    [DataType(DataType.Date)]
    public DateTime ScheduledDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Vui lòng chọn khung giờ.")]
    [Range(8, 20, ErrorMessage = "Khung giờ phải nằm trong khoảng 8h - 20h.")]
    public int SlotHour { get; set; } = 10;

    [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự.")]
    public string? Note { get; set; }

    public static IEnumerable<int> AvailableHours() => Enumerable.Range(8, 13); // 8..20
}
