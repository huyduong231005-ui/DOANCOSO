using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum HouseDirection
{
    East = 0,
    West = 1,
    South = 2,
    North = 3,
    NorthEast = 4,
    SouthEast = 5,
    NorthWest = 6,
    SouthWest = 7
}

// Tình trạng của căn do Ban quản lý đặt (cột TinhTrangCanHo).
public enum ApartmentCondition
{
    HandedOver = 0,
    NotHandedOver = 1,
    UnderMaintenance = 3
}

// Trạng thái hiển thị, không lưu: "Đang ở" / "Để trống" suy ra từ CuTru (căn có người chưa chuyển đi),
// để một thông tin chỉ nằm ở một bảng.
public enum ApartmentOccupancy
{
    Vacant = 0,
    NotHandedOver = 1,
    Occupied = 2,
    UnderMaintenance = 3
}

/// <summary>
/// Một căn hộ nằm trên một tầng. Toà của căn suy ra qua tầng (Floor.Building), không lưu lại ở căn
/// để một thông tin chỉ nằm ở một bảng. Mã căn (vd: A-12.05) bắt đầu bằng mã toà nên duy nhất toàn hệ thống.
/// </summary>
public class Apartment : BaseEntity
{
    public string UnitCode { get; set; } = string.Empty;
    public double Area { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public HouseDirection? HouseDirection { get; set; }
    public ApartmentCondition Condition { get; set; } = ApartmentCondition.HandedOver;
    public string? Note { get; set; }

    public int FloorId { get; set; }
    public Floor Floor { get; set; } = null!;

    public ICollection<ApartmentResident> Residents { get; set; } = new List<ApartmentResident>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<UtilityReading> UtilityReadings { get; set; } = new List<UtilityReading>();

    /// <summary>Dùng khi đã nạp Residents.</summary>
    public ApartmentOccupancy Occupancy => OccupancyOf(Condition, Residents.Any(r => r.MoveOutDate == null));

    public static ApartmentOccupancy OccupancyOf(ApartmentCondition c, bool hasResident) => c switch
    {
        ApartmentCondition.NotHandedOver => ApartmentOccupancy.NotHandedOver,
        ApartmentCondition.UnderMaintenance => ApartmentOccupancy.UnderMaintenance,
        _ => hasResident ? ApartmentOccupancy.Occupied : ApartmentOccupancy.Vacant
    };

    // Dạng biểu thức để EF dịch sang SQL (đếm, lọc).
    public static readonly System.Linq.Expressions.Expression<Func<Apartment, bool>> IsOccupied =
        a => a.Condition == ApartmentCondition.HandedOver && a.Residents.Any(r => r.MoveOutDate == null);

    public static readonly System.Linq.Expressions.Expression<Func<Apartment, bool>> IsVacant =
        a => a.Condition == ApartmentCondition.HandedOver && !a.Residents.Any(r => r.MoveOutDate == null);
    public ICollection<RecurringCharge> RecurringCharges { get; set; } = new List<RecurringCharge>();
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    /// <summary>Người đại diện căn để gửi thông báo phí (cần Include Residents).</summary>
    public ApartmentResident? PrimaryResident => Residents
        .Where(r => r.MoveOutDate == null)
        .OrderByDescending(r => r.IsPrimary)
        .ThenBy(r => r.Role)
        .FirstOrDefault();
}
