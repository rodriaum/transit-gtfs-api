using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FareLegRuleService : IFareLegRuleService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<FareLegRuleService> _logger;
    private readonly IRedisService _redis;

    public FareLegRuleService(GtfsDbContext gtfsDbContext, ILogger<FareLegRuleService> logger, IRedisService redis)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<FareLegRule>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<FareLegRule>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "fare_leg_rules.txt");

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
                await _gtfsDbContext.Set<FareLegRule>().Select(f => f.FareLegRuleId.ToLower()).ToListAsync()
            );

            List<FareLegRule> entities = new List<FareLegRule>(batchSize);

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

                    string fareLegRuleId = rowData.GetValueOrDefault("fare_leg_rule_id", "") ?? "";
                    string uniqueKey = fareLegRuleId.ToLower();
                    if (existingIds.Contains(uniqueKey))
                    {
                        totalIgnored++;
                        continue;
                    }

                    FareLegRule entity = new FareLegRule
                    {
                        Id = Guid.NewGuid().ToString(),
                        FareLegRuleId = fareLegRuleId,
                        FareProductId = rowData.GetValueOrDefault("fare_product_id", null),
                        LegGroupId = rowData.GetValueOrDefault("leg_group_id", null),
                        NetworkId = rowData.GetValueOrDefault("network_id", null)
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
