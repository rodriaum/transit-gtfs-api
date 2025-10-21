using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FareProductService : IFareProductService
{
    private readonly GtfsDbContext _gtfsDBContext;
    private readonly ILogger<FareProductService> _logger;
    private readonly IRedisService _redis;

    public FareProductService(GtfsDbContext gtfsDBContext, ILogger<FareProductService> logger, IRedisService redis)
    {
        _gtfsDBContext = gtfsDBContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<FareProduct>> GetAllAsync()
    {
        return await _gtfsDBContext.Set<FareProduct>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "fare_products.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.SqlBatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingIds = new HashSet<string>(
                await _gtfsDBContext.Set<FareProduct>().Select(f => f.FareProductId.ToLower()).ToListAsync()
            );

            List<FareProduct> entities = new List<FareProduct>(batchSize);

            using (StreamReader reader = new StreamReader(filePath))
            {
                string? headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    _logger.LogWarning($"No data found in {filePath}");
                    return;
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

                    string fareProductId = rowData.GetValueOrDefault("fare_product_id", "") ?? "";
                    string uniqueKey = fareProductId.ToLower();
                    if (existingIds.Contains(uniqueKey))
                    {
                        totalIgnored++;
                        continue;
                    }

                    FareProduct entity = new FareProduct
                    {
                        Id = Guid.NewGuid().ToString(),
                        FareProductId = fareProductId,
                        Name = rowData.GetValueOrDefault("name", "") ?? "",
                        Description = rowData.GetValueOrDefault("description", null),
                        Type = rowData.GetValueOrDefault("type", null),
                        Amount = decimal.TryParse(rowData.GetValueOrDefault("amount", null), out var amount) ? amount : null,
                        Currency = rowData.GetValueOrDefault("currency", null)
                    };

                    entities.Add(entity);
                    existingIds.Add(uniqueKey);

                    if (entities.Count >= batchSize)
                    {
                        await _gtfsDBContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _gtfsDBContext.BulkInsertAsync(entities);
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Inserted {0} records from {1} in database with {2} line(s) ignored. ({3})",
                totalImported,
                filePath,
                totalIgnored,
                TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            return;
        }
    }
}
