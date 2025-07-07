using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using TransitGtfsApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace TransitGtfsApi.Service;

public class FareRulesService : IFareRulesService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<FareRulesService> _logger;
    private readonly IRedisService _redis;

    public FareRulesService(TransitDbContext dbContext, ILogger<FareRulesService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<FareRule>> GetAllAsync()
    {
        return await _dbContext.FareRules.ToListAsync();
    }

    public async Task<List<FareRule>?> GetByFareIdAsync(string fareId)
    {
        return await _redis.GetOrSetAsync(
            $"fare-rules-{fareId}",
            async () => await _dbContext.FareRules.Where(f => f.FareId == fareId).ToListAsync()
        );
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "fare_rules.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Importing data from {filePath}");

            int batchSize = 1000;
            List<FareRule> entities = new List<FareRule>(batchSize);
            int totalImported = 0;

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

                    FareRule entity = new FareRule
                    {
                        Id = Guid.NewGuid().ToString(),
                        FareId = rowData.GetValueOrDefault("fare_id", "") ?? "",
                        RouteId = rowData.GetValueOrDefault("route_id", null),
                        OriginId = rowData.GetValueOrDefault("origin_id", null),
                        DestinationId = rowData.GetValueOrDefault("destination_id", null),
                        ContainsId = rowData.GetValueOrDefault("contains_id", null)
                    };

                    entities.Add(entity);

                    if (entities.Count >= batchSize)
                    {
                        _dbContext.FareRules.AddRange(entities);
                        await _dbContext.SaveChangesAsync();
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    _dbContext.FareRules.AddRange(entities);
                    await _dbContext.SaveChangesAsync();
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }

            _logger.LogInformation($"Imported {totalImported} records from {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            throw;
        }
    }
}