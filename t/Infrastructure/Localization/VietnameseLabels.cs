using t.Models.Entities;

namespace t.Infrastructure.Localization;

/// <summary>
/// Extension method Vi() ánh xạ enum sang nhãn tiếng Việt để hiển thị trên view.
/// Mục tiêu: không hiển thị tên enum tiếng Anh ở bất kỳ trang user-facing nào.
/// </summary>
public static class VietnameseLabels
{
    public static string Vi(this FurnishingLevel value) => value switch
    {
        FurnishingLevel.None => "Chưa có nội thất",
        FurnishingLevel.Basic => "Nội thất cơ bản",
        FurnishingLevel.FullyFurnished => "Đầy đủ nội thất",
        _ => value.ToString()
    };

    public static string Vi(this ParkingType value) => value switch
    {
        ParkingType.None => "Không có",
        ParkingType.Motorbike => "Xe máy",
        ParkingType.Car => "Ô tô",
        _ => value.ToString()
    };

    public static string Vi(this HouseDirection value) => value switch
    {
        HouseDirection.East => "Đông",
        HouseDirection.West => "Tây",
        HouseDirection.South => "Nam",
        HouseDirection.North => "Bắc",
        HouseDirection.NorthEast => "Đông Bắc",
        HouseDirection.SouthEast => "Đông Nam",
        HouseDirection.NorthWest => "Tây Bắc",
        HouseDirection.SouthWest => "Tây Nam",
        _ => value.ToString()
    };

    public static string Vi(this LeaseStatus s) => s switch
    {
        LeaseStatus.Pending => "Chờ kích hoạt",
        LeaseStatus.Active => "Đang hiệu lực",
        LeaseStatus.Renewing => "Đang gia hạn",
        LeaseStatus.Expired => "Đã hết hạn",
        LeaseStatus.Terminated => "Đã chấm dứt",
        _ => s.ToString()
    };

    public static string Vi(this BuildingStatus s) => s switch
    {
        BuildingStatus.Active => "Đang vận hành",
        BuildingStatus.UnderMaintenance => "Đang bảo trì",
        BuildingStatus.Closed => "Đã đóng",
        _ => s.ToString()
    };

    public static string Vi(this ApartmentOccupancy o) => o switch
    {
        ApartmentOccupancy.Available => "Còn trống",
        ApartmentOccupancy.Reserved => "Đã đặt giữ",
        ApartmentOccupancy.Occupied => "Đang có khách",
        ApartmentOccupancy.UnderMaintenance => "Đang bảo trì",
        _ => o.ToString()
    };

    public static string Vi(this ListingStatus s) => s switch
    {
        ListingStatus.Draft => "Bản nháp",
        ListingStatus.Active => "Đang đăng",
        ListingStatus.Expired => "Hết hạn đăng",
        ListingStatus.Hidden => "Đã ẩn",
        _ => s.ToString()
    };

    public static string Vi(this ProjectStatus s) => s switch
    {
        ProjectStatus.Upcoming => "Sắp mở",
        ProjectStatus.OpenForRent => "Đang mở thuê",
        ProjectStatus.Completed => "Đã hoàn thiện",
        _ => s.ToString()
    };

    public static string Vi(this InvoiceStatus s) => s switch
    {
        InvoiceStatus.Draft => "Bản nháp",
        InvoiceStatus.Issued => "Đã phát hành",
        InvoiceStatus.PartiallyPaid => "Trả một phần",
        InvoiceStatus.Paid => "Đã thanh toán",
        InvoiceStatus.Overdue => "Quá hạn",
        InvoiceStatus.Cancelled => "Đã huỷ",
        InvoiceStatus.Refunded => "Đã hoàn tiền",
        _ => s.ToString()
    };

    public static string Vi(this InvoiceKind k) => k switch
    {
        InvoiceKind.MonthlyRent => "Tiền thuê tháng",
        InvoiceKind.Deposit => "Tiền cọc",
        InvoiceKind.LateFee => "Phí trễ hạn",
        InvoiceKind.OneOff => "Phát sinh",
        _ => k.ToString()
    };

    public static string Vi(this PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => "Chờ xác nhận",
        PaymentStatus.Succeeded => "Thành công",
        PaymentStatus.Failed => "Thất bại",
        PaymentStatus.Cancelled => "Đã huỷ",
        PaymentStatus.Refunded => "Đã hoàn",
        PaymentStatus.PartiallyRefunded => "Hoàn một phần",
        _ => s.ToString()
    };

    public static string Vi(this PaymentMethod m) => m switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        PaymentMethod.CreditCard => "Thẻ tín dụng",
        PaymentMethod.Wallet => "Ví điện tử",
        PaymentMethod.QrCode => "Quét QR",
        _ => m.ToString()
    };

    public static string Vi(this ReviewStatus s) => s switch
    {
        ReviewStatus.Pending => "Chờ duyệt",
        ReviewStatus.Approved => "Đã duyệt",
        ReviewStatus.Rejected => "Đã từ chối",
        _ => s.ToString()
    };

    public static string Vi(this AuditAction a) => a switch
    {
        AuditAction.Create => "Tạo mới",
        AuditAction.Update => "Cập nhật",
        AuditAction.Delete => "Xoá cứng",
        AuditAction.SoftDelete => "Xoá mềm",
        AuditAction.Restore => "Khôi phục",
        AuditAction.Login => "Đăng nhập",
        AuditAction.Logout => "Đăng xuất",
        _ => a.ToString()
    };

    public static string Vi(this MaintenanceCategory c) => c switch
    {
        MaintenanceCategory.Plumbing => "Cấp / thoát nước",
        MaintenanceCategory.Electrical => "Điện",
        MaintenanceCategory.Appliance => "Thiết bị",
        MaintenanceCategory.Structural => "Kết cấu",
        MaintenanceCategory.Cleaning => "Vệ sinh",
        MaintenanceCategory.Pest => "Côn trùng",
        MaintenanceCategory.Network => "Mạng / Internet",
        MaintenanceCategory.Other => "Khác",
        _ => c.ToString()
    };

    public static string Vi(this MaintenancePriority p) => p switch
    {
        MaintenancePriority.Low => "Thấp",
        MaintenancePriority.Medium => "Trung bình",
        MaintenancePriority.High => "Cao",
        MaintenancePriority.Urgent => "Khẩn cấp",
        _ => p.ToString()
    };

    public static string Vi(this MaintenanceStatus s) => s switch
    {
        MaintenanceStatus.Open => "Mới gửi",
        MaintenanceStatus.Acknowledged => "Đã tiếp nhận",
        MaintenanceStatus.InProgress => "Đang xử lí",
        MaintenanceStatus.Resolved => "Đã hoàn tất",
        MaintenanceStatus.Closed => "Đã đóng",
        MaintenanceStatus.Rejected => "Đã từ chối",
        _ => s.ToString()
    };

    public static string Vi(this InspectionType t) => t switch
    {
        InspectionType.MoveIn => "Nhận nhà",
        InspectionType.MoveOut => "Trả nhà",
        _ => t.ToString()
    };

    public static string Vi(this OverallCondition c) => c switch
    {
        OverallCondition.Excellent => "Xuất sắc",
        OverallCondition.Good => "Tốt",
        OverallCondition.Fair => "Trung bình",
        OverallCondition.Poor => "Kém",
        _ => c.ToString()
    };

    public static string Vi(this DepositTransactionType t) => t switch
    {
        DepositTransactionType.Hold => "Nhận cọc",
        DepositTransactionType.Refund => "Hoàn cọc",
        DepositTransactionType.Forfeit => "Mất cọc",
        DepositTransactionType.Deduction => "Trừ cọc",
        DepositTransactionType.Adjustment => "Điều chỉnh",
        _ => t.ToString()
    };

    public static string Vi(this UtilityBillingMode m) => m switch
    {
        UtilityBillingMode.Metered => "Theo đồng hồ",
        UtilityBillingMode.Fixed => "Cố định",
        _ => m.ToString()
    };

    public static string Vi(this ViewingStatus s) => s switch
    {
        ViewingStatus.Pending => "Chờ xác nhận",
        ViewingStatus.Confirmed => "Đã xác nhận",
        ViewingStatus.Cancelled => "Đã huỷ",
        ViewingStatus.Completed => "Đã xem",
        ViewingStatus.NoShow => "Không đến",
        _ => s.ToString()
    };
}
