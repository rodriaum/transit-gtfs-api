namespace Tranzor.Utils;

public class NumberUtil
{
    public static int ParseIntSafe(object? value, int? defaultValue = null)
    {
        if (value == null)
            return defaultValue ?? 0;

        string? str = value.ToString();

        if (string.IsNullOrWhiteSpace(str))
            return defaultValue ?? 0;

        return int.Parse(str);
    }

    public static decimal ParseDecimalSafe(object? value, decimal? defaultValue = null, IFormatProvider? format = null)
    {
        if (value == null)
            return defaultValue ?? 0;

        string? str = value.ToString();

        if (string.IsNullOrWhiteSpace(str))
            return defaultValue ?? 0;

        return decimal.Parse(str, format);
    }

    public static double ParseDoubleSafe(object? value, double? defaultValue = null, IFormatProvider? format = null)
    {
        if (value == null)
            return defaultValue ?? 0;

        string? str = value.ToString();

        if (string.IsNullOrWhiteSpace(str))
            return defaultValue ?? 0;

        return double.Parse(str, format);
    }

    public static bool isValidNumber(double n)
    {
        return !double.IsInfinity(n) && !double.IsNaN(n);
    }

    public static TimeSpan ParseGtfsTime(string timeString)
    {
        string[] parts = timeString.Split(':');

        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out int hours) ||
            !int.TryParse(parts[1], out int minutes) ||
            !int.TryParse(parts[2], out int seconds))
        {
            throw new FormatException("Hora em formato inválido. Esperado: HH:mm:ss");
        }

        return new TimeSpan(hours, minutes, seconds);
    }

}