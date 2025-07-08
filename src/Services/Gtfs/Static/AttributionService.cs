using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Services.Gtfs.Static;

public class AttributionService : IAttributionService
{
    private readonly GTFSContext _dbContext;
    private readonly ILogger<AttributionService> _logger;
    private readonly IRedisService _redis;

    public AttributionService(GTFSContext dbContext, ILogger<AttributionService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Attribution>> GetAllAsync()
    {
        return await _dbContext.Set<Attribution>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "attributions.txt");

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
                await _dbContext.Set<Attribution>().Select(a => (a.AgencyId ?? "") + ":" + (a.RouteId ?? "") + ":" + (a.TripId ?? "") + ":" + a.OrganizationName.ToLower()).ToListAsync()
            );

            List<Attribution> entities = new List<Attribution>(batchSize);

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

                    string agencyId = rowData.GetValueOrDefault("agency_id", null) ?? "";
                    string routeId = rowData.GetValueOrDefault("route_id", null) ?? "";
                    string tripId = rowData.GetValueOrDefault("trip_id", null) ?? "";
                    string orgName = rowData.GetValueOrDefault("organization_name", "") ?? "";
                    string uniqueKey = agencyId + ":" + routeId + ":" + tripId + ":" + orgName.ToLower();
                    if (existingIds.Contains(uniqueKey))
                    {
                        totalIgnored++;
                        continue;
                    }

                    Attribution entity = new Attribution
                    {
                        Id = Guid.NewGuid().ToString(),
                        AgencyId = agencyId,
                        RouteId = routeId,
                        TripId = tripId,
                        OrganizationName = orgName,
                        IsProducer = rowData.GetValueOrDefault("is_producer", "0") == "1",
                        IsOperator = rowData.GetValueOrDefault("is_operator", "0") == "1",
                        IsAuthority = rowData.GetValueOrDefault("is_authority", "0") == "1",
                        AttributionUrl = rowData.GetValueOrDefault("attribution_url", null),
                        AttributionEmail = rowData.GetValueOrDefault("attribution_email", null),
                        AttributionPhone = rowData.GetValueOrDefault("attribution_phone", null)
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
                $"Inserted {totalImported} records from {filePath} in database with {totalIgnored} file(s) ignored. ({{0}})",
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
