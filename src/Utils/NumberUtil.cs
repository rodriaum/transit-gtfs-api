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
}