namespace t.Infrastructure.Time;

/// <summary>
/// Vietnam local time helper. Use for "today/now" in business logic that the user
/// reasons about in local time (lease expiry, billing day, default dates).
/// Audit timestamps (CreatedAt, RecordedAt, AuditLog) should keep using DateTime.UtcNow.
/// </summary>
public static class VnTime
{
    private static readonly TimeZoneInfo Tz = ResolveTimeZone();

    /// <summary>Current Vietnam local time as DateTime (Kind=Unspecified).</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);

    /// <summary>Current Vietnam local date (no time component).</summary>
    public static DateTime Today => Now.Date;

    /// <summary>Convert a UTC instant to Vietnam local time.</summary>
    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tz);

    private static TimeZoneInfo ResolveTimeZone()
    {
        // Linux/macOS uses IANA "Asia/Ho_Chi_Minh"; Windows uses "SE Asia Standard Time".
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        // Fallback: hardcoded UTC+7 (VN has no DST).
        return TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Vietnam", "Vietnam");
    }
}
