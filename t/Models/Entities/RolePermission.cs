using Microsoft.AspNetCore.Identity;
using t.Models.Entities.Common;

namespace t.Models.Entities;

public class RolePermission : IAuditable
{
    public string RoleId { get; set; } = string.Empty;
    public IdentityRole Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
