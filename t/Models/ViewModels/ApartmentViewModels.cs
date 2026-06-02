using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using t.Infrastructure.ModelBinding;
using t.Models.Entities;

namespace t.Models.ViewModels;

public class ApartmentListViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double Area { get; set; }
    public int Bedrooms { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Badge { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public List<string> AmenityIcons { get; set; } = new();
    public List<string> AmenityNames { get; set; } = new();
    public bool IsFavorite { get; set; }
    public double? DistanceKm { get; set; }
}

public class ApartmentListPageViewModel
{
    public List<ApartmentListViewModel> Apartments { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public string? RegionSlug { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinArea { get; set; }
    public double? MaxArea { get; set; }
    public List<int>? CategoryIds { get; set; }
    public List<int>? AmenityIds { get; set; }
    public string? SortBy { get; set; }
    public string? CategorySlug { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsNearbySort =>
        SortBy == "distance_asc" &&
        Latitude.HasValue &&
        Longitude.HasValue;
}

public class CreateApartmentViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề.")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Tiêu đề phải từ 10 đến 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(5000, MinimumLength = 20, ErrorMessage = "Mô tả phải từ 20 đến 5000 ký tự.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại hình.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại hình.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập diện tích.")]
    [Range(5, 10000, ErrorMessage = "Diện tích phải từ 5 đến 10.000 m².")]
    public double Area { get; set; }

    [Range(0, 20, ErrorMessage = "Số phòng ngủ từ 0 đến 20.")]
    public int Bedrooms { get; set; }

    [Range(0, 20, ErrorMessage = "Số phòng tắm từ 0 đến 20.")]
    public int Bathrooms { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá thuê.")]
    [Range(500_000d, 1_000_000_000d, ErrorMessage = "Giá thuê phải từ 500.000đ đến 1 tỷ đồng/tháng.")]
    public decimal Price { get; set; }

    [Range(0d, 5_000_000_000d, ErrorMessage = "Tiền cọc không hợp lệ.")]
    public decimal? DefaultDeposit { get; set; }

    [StringLength(300)]
    public string? FeeNote { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(300, MinimumLength = 10, ErrorMessage = "Địa chỉ phải từ 10 đến 300 ký tự.")]
    public string Address { get; set; } = string.Empty;

    [ModelBinder(BinderType = typeof(InvariantNullableDoubleModelBinder))]
    public double? Latitude { get; set; }

    [ModelBinder(BinderType = typeof(InvariantNullableDoubleModelBinder))]
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khu vực.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khu vực.")]
    public int RegionId { get; set; }

    public int? ProjectId { get; set; }
    public List<int> AmenityIds { get; set; } = new();
    public List<IFormFile> Images { get; set; } = new();

    [Range(0, 14, ErrorMessage = "Vị trí ảnh bìa không hợp lệ.")]
    public int CoverImageIndex { get; set; }

    public List<Category> Categories { get; set; } = new();
    public List<Region> Regions { get; set; } = new();
    public List<Amenity> Amenities { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
}
