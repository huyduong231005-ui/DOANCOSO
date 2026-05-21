namespace t.Models.Entities;

public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    SoftDelete = 3,
    Restore = 4,
    Login = 5,
    Logout = 6
}

public class AuditLog
{
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityKey { get; set; }
    public AuditAction Action { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedColumns { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
