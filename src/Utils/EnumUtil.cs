namespace Tranzor.Utils;

using System;

public static class EnumUtil
{
    /// <summary>
    /// Converts a string to enum based on name
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <param name="name">Enum value name</param>
    /// <param name="ignoreCase">Whether to ignore case sensitivity (default: false)</param>
    /// <returns>Converted enum value or null if invalid</returns>
    public static T? FromName<T>(string name, bool ignoreCase = false) where T : struct, Enum
    {
        if (name == null)
            return null;

        if (Enum.TryParse(name, ignoreCase, out T result))
            return result;

        return null;
    }

    /// <summary>
    /// Tries to convert a string to enum based on name
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <param name="name">Enum value name</param>
    /// <param name="result">Converted enum value (if successful)</param>
    /// <param name="ignoreCase">Whether to ignore case sensitivity (default: false)</param>
    /// <returns>True if conversion was successful, false otherwise</returns>
    public static bool TryFromName<T>(string name, out T result, bool ignoreCase = false) where T : struct, Enum
    {
        return Enum.TryParse<T>(name, ignoreCase, out result);
    }

    /// <summary>
    /// Converts a numeric value to enum
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <param name="value">Numeric value</param>
    /// <returns>Converted enum value or null if invalid</returns>
    public static T? FromValue<T>(int value) where T : struct, Enum
    {
        if (Enum.IsDefined(typeof(T), value))
            return (T)Enum.ToObject(typeof(T), value);

        return null;
    }

    /// <summary>
    /// Converts a numeric value to enum (generic version for other numeric types)
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <param name="value">Numeric value</param>
    /// <returns>Converted enum value or null if invalid</returns>
    public static T? FromValue<T>(object value) where T : struct, Enum
    {
        if (value == null)
            return null;

        if (Enum.IsDefined(typeof(T), value))
            return (T)Enum.ToObject(typeof(T), value);

        return null;
    }

    /// <summary>
    /// Tries to convert a numeric value to enum
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <param name="value">Numeric value</param>
    /// <param name="result">Converted enum value (if successful)</param>
    /// <returns>True if conversion was successful, false otherwise</returns>
    public static bool TryFromValue<T>(int value, out T result) where T : struct, Enum
    {
        if (Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default(T);
        return false;
    }

    /// <summary>
    /// Tries to convert a numeric value to enum (generic version)
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <param name="value">Numeric value</param>
    /// <param name="result">Converted enum value (if successful)</param>
    /// <returns>True if conversion was successful, false otherwise</returns>
    public static bool TryFromValue<T>(object value, out T result) where T : struct, Enum
    {
        if (value != null && Enum.IsDefined(typeof(T), value))
        {
            result = (T)Enum.ToObject(typeof(T), value);
            return true;
        }

        result = default(T);
        return false;
    }

    /// <summary>
    /// Gets the numeric value of an enum
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <param name="enumValue">Enum value</param>
    /// <returns>Numeric value of the enum</returns>
    public static int ToValue<T>(T enumValue) where T : struct, Enum
    {
        return Convert.ToInt32(enumValue);
    }
}