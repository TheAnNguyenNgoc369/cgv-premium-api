namespace CinemaBooking.Shared.Time;

public static class VietnamTime
{
    public const int UtcOffsetHours = 7;
    public static TimeSpan UtcOffset { get; } = TimeSpan.FromHours(UtcOffsetHours);

    public static DateTimeOffset FromUtc(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return new DateTimeOffset(utc).ToOffset(UtcOffset);
    }

    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = new DateTimeOffset(date.ToDateTime(time), UtcOffset);
        return local.UtcDateTime;
    }

    public static DateOnly GetDate(DateTime utcNow) =>
        DateOnly.FromDateTime(FromUtc(utcNow).DateTime);

    public static (DateTime FromUtc, DateTime ToUtc) GetUtcDayRange(DateOnly vietnamDate)
    {
        var fromUtc = ToUtc(vietnamDate, TimeOnly.MinValue);
        return (fromUtc, fromUtc.AddDays(1));
    }
}
