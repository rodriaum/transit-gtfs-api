/*
 * Copyright (C) Rodrigo Ferreira, All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 */

using System.Text;
using System.Text.Json;

namespace Tranzor.Utils;

public class JsonUtil
{
    public static async Task<string> ObjectToStringAsync<T>(object value, JsonSerializerOptions? options = null)
    {
        using MemoryStream stream = new MemoryStream();

        await JsonSerializer.SerializeAsync(stream, value, options);
        stream.Position = 0;

        using StreamReader reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }

    public static async Task<T?> StringToObjectAsync<T>(string json)
    {
        try
        {
            using var steam = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return await JsonSerializer.DeserializeAsync<T>(steam);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}