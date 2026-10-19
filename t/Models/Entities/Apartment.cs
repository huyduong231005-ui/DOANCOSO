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

// Tình trạng sử dụng căn hộ theo góc nhìn Ban quản lý.
public enum ApartmentOccupancy
{
    Vacant = 0,
    NotHandedOver = 1,
    Occupied = 2,
    UnderMaintenance = 3
}

/// <summary>Một căn hộ nằm trên một tầng của một toà. Mã căn duy nhất trong toà (vd: A-12.05).</summary>
public class Apartment : BaseEntity
{
    public string UnitCode { get; set; } = string.Empty;
    public double Area { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public HouseDirection? HouseDirection { get; set; }
    public ApartmentOccupancy Occupancy { get; set; } = ApartmentOccupancy.Vacant;
    public string? Note { get; set; }

    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public int FloorId { get; set; }
    public Floor Floor { get; set; } = null!;

    public ICollection<ApartmentResident> Residents { get; set; } = new List<ApartmentResident>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<UtilityReading> UtilityReadings { get; set; } = new List<UtilityReading>();
    public ICollection<RecurringCharge> RecurringCharges { get; set; } = new List<RecurringCharge>();
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    /// <summary>Người đại diện căn để gửi thông báo phí (cần Include Residents).</summary>
    public ApartmentResident? PrimaryResident => Residents
        .Where(r => r.MoveOutDate == null)
        .OrderByDescending(r => r.IsPrimary)
        .ThenBy(r => r.Role)
        .FirstOrDefault();
}
