using System.ComponentModel.DataAnnotations;
using t.Models.Entities;

namespace t.Models.ViewModels;

public class MyListingItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public double Area { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public ListingStatus Status { get; set; }
    public ApartmentOccupancy Occupancy { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModerationNote { get; set; }
}

public class MyListingsPageViewModel
{
    public List<MyListingItemViewModel> Items { get; set; } = new();
    public int Total { get; set; }
    public int Active { get; set; }
    public int Hidden { get; set; }
}

public class EditListingViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Tiêu đề phải từ 10 đến 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(5000, MinimumLength = 20, ErrorMessage = "Mô tả phải từ 20 đến 5000 ký tự.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá thuê.")]
    [Range(500_000d, 1_000_000_000d, ErrorMessage = "Giá thuê từ 500.000đ đến 1 tỷ đồng/tháng.")]
    public decimal Price { get; set; }

    [Range(0d, 5_000_000_000d, ErrorMessage = "Tiền cọc không hợp lệ.")]
    public decimal? DefaultDeposit { get; set; }

    [StringLength(300)]
    public string? FeeNote { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(300, MinimumLength = 10)]
    public string Address { get; set; } = string.Empty;

    [Range(5, 10000)]
    public double Area { get; set; }

    [Range(0, 20)]
    public int Bedrooms { get; set; }

    [Range(0, 20)]
    public int Bathrooms { get; set; }

    public FurnishingLevel FurnishingLevel { get; set; }
    public bool AllowsPets { get; set; }
    public ParkingType ParkingType { get; set; }
    public DateOnly AvailableFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [Range(1, 120)] public int MinLeaseMonths { get; set; } = 1;
    [Range(1, 120)] public int MaxLeaseMonths { get; set; } = 12;
    public HouseDirection? HouseDirection { get; set; }
    [Range(0, 500)] public int? FloorNumber { get; set; }

    public ListingStatus Status { get; set; }

    public string? CoverImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
