using System.Text;

namespace TransitGtfsApi.Utils;

public static class TimeFormatUtil
{
    public static string FormatDurationFromMilliseconds(long milliseconds)
    {
        if (milliseconds < 0) return "0 milliseconds";

        var sb = new StringBuilder();
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

        void AppendUnit(long value, string singular, string plural)
        {
            if (value > 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(value).Append(' ').Append(value == 1 ? singular : plural);
            }
        }

        AppendUnit(years, "year", "years");
        AppendUnit(months, "month", "months");
        AppendUnit(weeks, "week", "weeks");
        AppendUnit(days, "day", "days");
        AppendUnit(hours, "hour", "hours");
        AppendUnit(minutes, "minute", "minutes");
        AppendUnit(seconds, "second", "seconds");
        AppendUnit(remainingMilliseconds, "millisecond", "milliseconds");

        if (sb.Length == 0)
            return "0 milliseconds";

        string result = sb.ToString();
        int lastComma = result.LastIndexOf(", ");

        if (lastComma != -1)
        {
            result = result.Remove(lastComma, 2).Insert(lastComma, " and ");
        }

        return result;
    }
}
