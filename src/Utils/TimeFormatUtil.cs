using System.Text;

namespace Tranzor.Utils;

public static class TimeFormatUtil
{
    public static string FormatDurationFromMilliseconds(long milliseconds, bool simplified = true)
    {
        if (milliseconds < 0) return simplified ? "0ms" : "0 milliseconds";

        StringBuilder sb = new StringBuilder();
        
        long totalSeconds = milliseconds / 1000;
        long remainingMilliseconds = milliseconds % 1000;
        long years = totalSeconds / (365 * 24 * 3600);
        totalSeconds %= (365 * 24 * 3600);
        long months = totalSeconds / (30 * 24 * 3600);
        totalSeconds %= (30 * 24 * 3600);
        long weeks = totalSeconds / (7 * 24 * 3600);
        totalSeconds %= (7 * 24 * 3600);
        long days = totalSeconds / (24 * 3600);
        totalSeconds %= (24 * 3600);
        long hours = totalSeconds / 3600;
        totalSeconds %= 3600;
        long minutes = totalSeconds / 60;
        totalSeconds %= 60;
        long seconds = totalSeconds;

        void AppendUnit(long value, string fullSingular, string fullPlural, string shortForm)
        {
            if (value > 0)
            {
                if (sb.Length > 0) sb.Append(simplified ? " " : ", ");
                if (simplified)
                {
                    sb.Append(value).Append(shortForm);
                }
                else
                {
                    sb.Append(value).Append(' ').Append(value == 1 ? fullSingular : fullPlural);
                }
            }
        }

        AppendUnit(years, "year", "years", "y");
        AppendUnit(months, "month", "months", "mo");
        AppendUnit(weeks, "week", "weeks", "w");
        AppendUnit(days, "day", "days", "d");
        AppendUnit(hours, "hour", "hours", "h");
        AppendUnit(minutes, "minute", "minutes", "m");
        AppendUnit(seconds, "second", "seconds", "s");
        AppendUnit(remainingMilliseconds, "millisecond", "milliseconds", "ms");

        if (sb.Length == 0)
            return simplified ? "0ms" : "0 milliseconds";

        string result = sb.ToString();

        if (!simplified)
        {
            int lastComma = result.LastIndexOf(", ");
            if (lastComma != -1)
            {
                result = result.Remove(lastComma, 2).Insert(lastComma, " and ");
            }
        }

        return result;
    }

    public static TimeSpan ParseGtfsTime(string timeString)
    {
        string[] parts = timeString.Split(':');

        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out int hours) ||
            !int.TryParse(parts[1], out int minutes) ||
            !int.TryParse(parts[2], out int seconds))
        {
            return TimeSpan.Zero;
        }

        return new TimeSpan(hours, minutes, seconds);
    }

    public static DateTime ParseGtfsDate(string dateString)
    {
        if (dateString.Length != 8)
        {
            return DateTime.MinValue;
        }

        int year = int.Parse(dateString.Substring(0, 4));
        int month = int.Parse(dateString.Substring(4, 2));
        int day = int.Parse(dateString.Substring(6, 2));

        return new DateTime(year, month, day);
    }
}
