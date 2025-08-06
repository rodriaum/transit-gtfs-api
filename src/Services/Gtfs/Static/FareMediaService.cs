using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Tranzor.Databases;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FareMediaService : IFareMediaService
{
    private readonly GTFSContext _dbContext;
    private readonly ILogger<FareMediaService> _logger;
    private readonly IRedisService _redis;

    public FareMediaService(GTFSContext dbContext, ILogger<FareMediaService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<FareMedia>> GetAllAsync()
    {
        return await _dbContext.Set<FareMedia>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "fare_media.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.BatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingIds = new HashSet<string>(
                await _dbContext.Set<FareMedia>().Select(f => f.FareMediaId.ToLower()).ToListAsync()
            );

            List<FareMedia> entities = new List<FareMedia>(batchSize);

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

                    string fareMediaId = rowData.GetValueOrDefault("fare_media_id", "") ?? "";
                    string uniqueKey = fareMediaId.ToLower();
                    if (existingIds.Contains(uniqueKey))
                    {
                        totalIgnored++;
                        continue;
                    }

                    FareMedia entity = new FareMedia
                    {
                        Id = Guid.NewGuid().ToString(),
                        FareMediaId = fareMediaId,
                        Name = rowData.GetValueOrDefault("name", "") ?? "",
                        Description = rowData.GetValueOrDefault("description", null),
                        Type = rowData.GetValueOrDefault("type", null)
                    };

                    entities.Add(entity);
                    existingIds.Add(uniqueKey);

                    if (entities.Count >= batchSize)
                    {
                        await _dbContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _dbContext.BulkInsertAsync(entities);
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                $"Inserted {totalImported} records from {filePath} in database with {totalIgnored} line(s) ignored. ({{0}})",
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
