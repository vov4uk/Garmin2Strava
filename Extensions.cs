using System;

internal static class Extensions
{
    public static double DateTimeToUnixTimestamp(this DateTime dateTime) => (TimeZoneInfo.ConvertTimeToUtc(dateTime) - DateTime.UnixEpoch).TotalSeconds;
}