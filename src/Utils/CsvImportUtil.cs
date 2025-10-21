namespace Tranzor.Utils;

public static class CsvImportUtil
{

    /// <summary>
    /// Read all lines from a CSV file and return as a list of dictionaries
    /// </summary>
    /// <param name="filePath">CSV file path</param>
    /// <param name="logger">Logger to register the process</param>
    /// <returns>List of dictionaries with CSV data</returns>
    public static async Task<List<Dictionary<string, string?>>> ReadCsvAsync(string filePath, ILogger logger)
    {
        List<Dictionary<string, string?>> rows = new List<Dictionary<string, string?>>();

        if (!File.Exists(filePath))
        {
            logger.LogWarning("File not found: {FilePath}", filePath);
            return rows;
        }

        try
        {
            logger.LogInformation("Reading CSV file: {FilePath}", filePath);

            using StreamReader reader = new StreamReader(filePath);
            string? headerLine = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                logger.LogWarning("No data found in {FilePath}", filePath);
                return rows;
            }

            string[] headers = headerLine.Split(',');
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');
                Dictionary<string, string?> rowData = new Dictionary<string, string?>();

                for (int j = 0; j < headers.Length; j++)
                {
                    if (j < values.Length)
                    {
                        rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
                    }
                }

                rows.Add(rowData);
            }

            logger.LogInformation("Read {Count} rows from {FilePath}", rows.Count, filePath);
            return rows;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading CSV file: {FilePath}", filePath);
            return rows;
        }
    }
}

