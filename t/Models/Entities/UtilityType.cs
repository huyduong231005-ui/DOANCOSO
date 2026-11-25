using System.ComponentModel.DataAnnotations;
using t.Models.Entities.Common;

namespace t.Models.Entities;

/// <summary>Cách tính số lượng của một loại dịch vụ trên hoá đơn.</summary>
public enum BillingMethod
{
    [Display(Name = "Theo diện tích")] ByArea = 0,       // theo diện tích căn (phí quản lý)
    [Display(Name = "Theo chỉ số")] ByMeter = 1,      // theo chỉ số đồng hồ (điện, nước)
    [Display(Name = "Theo đăng ký")] BySubscription = 2, // theo số lượng căn đăng ký (gửi xe)
    [Display(Name = "Theo lần")] PerUse = 3        // theo lần phát sinh (sửa chữa)
}

/// <summary>Bảng giá: mọi khoản thu đều là một loại dịch vụ, đơn giá chỉ nằm ở đây.</summary>
public class UtilityType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal DefaultRate { get; set; }
    public BillingMethod Method { get; set; } = BillingMethod.ByMeter;

    /// <summary>Mã loại dịch vụ phí quản lý và sửa chữa, dùng khi lập hoá đơn.</summary>
    public const string ManagementCode = "QL", RepairCode = "SC";

    public ICollection<UtilityReading> Readings { get; set; } = new List<UtilityReading>();
}
