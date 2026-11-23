namespace HardwareStore.Services;

/// <summary>
/// Application clock. Belarus keeps UTC+3 all year round (no daylight saving),
/// so every timestamp written by the system uses this offset.
/// </summary>
public static class AppTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(3);

    /// <summary>Current local time for the platform (UTC+3).</summary>
    public static DateTime Now => DateTime.UtcNow.Add(Offset);

    /// <summary>Current local date, used for daily aggregation.</summary>
    public static DateTime Today => Now.Date;

    /// <summary>Converts a stored UTC timestamp into local time when displaying legacy data.</summary>
    public static DateTime FromUtc(DateTime utc) => utc.Add(Offset);
}
