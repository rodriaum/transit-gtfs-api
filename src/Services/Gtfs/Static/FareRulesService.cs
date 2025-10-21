using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FareRulesService : IFareRulesService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<FareRulesService> _logger;
    private readonly IRedisService _redis;

    public FareRulesService(GtfsDbContext gtfsDbContext, ILogger<FareRulesService> logger, IRedisService redis)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<FareRule>> GetAllAsync()
    {
        return await _gtfsDbContext.FareRules.ToListAsync();
    }

    public async Task<List<FareRule>?> GetByFareIdAsync(string fareId)
    {
        return await _redis.GetOrSetAsync(
            $"fare-rules-{fareId}",
            async () => await _gtfsDbContext.FareRules.Where(f => f.FareId == fareId).ToListAsync()
        );
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "fare_rules.txt");

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
                await _gtfsDbContext.FareRules.Select(f => f.FareId.ToLower() + ":" + (f.RouteId ?? "").ToLower() + ":" + (f.OriginId ?? "").ToLower() + ":" + (f.DestinationId ?? "").ToLower() + ":" + (f.ContainsId ?? "").ToLower()).ToListAsync()
            );

            List<FareRule> entities = new List<FareRule>(batchSize);

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

                    string fareId = rowData.GetValueOrDefault("fare_id", "") ?? "";
                    string routeId = rowData.GetValueOrDefault("route_id", null) ?? "";
                    string originId = rowData.GetValueOrDefault("origin_id", null) ?? "";
                    string destinationId = rowData.GetValueOrDefault("destination_id", null) ?? "";
                    string containsId = rowData.GetValueOrDefault("contains_id", null) ?? "";
                    string uniqueKey = fareId.ToLower() + ":" + routeId.ToLower() + ":" + originId.ToLower() + ":" + destinationId.ToLower() + ":" + containsId.ToLower();
                    if (existingIds.Contains(uniqueKey))
                    {
                        totalIgnored++;
                        continue;
                    }

                    FareRule entity = new FareRule
                    {
                        Id = Guid.NewGuid().ToString(),
                        FareId = fareId,
                        RouteId = string.IsNullOrEmpty(routeId) ? null : routeId,
                        OriginId = string.IsNullOrEmpty(originId) ? null : originId,
                        DestinationId = string.IsNullOrEmpty(destinationId) ? null : destinationId,
                        ContainsId = string.IsNullOrEmpty(containsId) ? null : containsId
                    };

                    entities.Add(entity);
                    existingIds.Add(uniqueKey);

                    if (entities.Count >= batchSize)
                    {
                        await _gtfsDbContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _gtfsDbContext.BulkInsertAsync(entities);
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