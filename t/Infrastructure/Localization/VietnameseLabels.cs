using t.Models.Entities;

namespace t.Infrastructure.Localization;

/// <summary>
/// Extension method Vi() ánh xạ enum sang nhãn tiếng Việt để hiển thị trên view.
/// Mục tiêu: không hiển thị tên enum tiếng Anh ở bất kỳ trang user-facing nào.
/// </summary>
public static class VietnameseLabels
{
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

    public static string Vi(this BuildingStatus s) => s switch
    {
        BuildingStatus.Active => "Đang vận hành",
        BuildingStatus.UnderMaintenance => "Đang bảo trì",
        BuildingStatus.Closed => "Đã đóng",
        _ => s.ToString()
    };

    public static string Vi(this ApartmentOccupancy o) => o switch
    {
        ApartmentOccupancy.Vacant => "Để trống",
        ApartmentOccupancy.NotHandedOver => "Chưa bàn giao",
        ApartmentOccupancy.Occupied => "Đang ở",
        ApartmentOccupancy.UnderMaintenance => "Đang sửa chữa",
        _ => o.ToString()
    };

    public static string Vi(this ResidentRole r) => r switch
    {
        ResidentRole.Owner => "Chủ sở hữu",
        ResidentRole.Tenant => "Người thuê",
        ResidentRole.Member => "Thành viên",
        _ => r.ToString()
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
        InvoiceKind.Monthly => "Phí dịch vụ tháng",
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

    public static string Vi(this UtilityBillingMode m) => m switch
    {
        UtilityBillingMode.Metered => "Theo đồng hồ",
        UtilityBillingMode.Fixed => "Cố định",
        _ => m.ToString()
    };

}
