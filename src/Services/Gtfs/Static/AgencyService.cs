using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Services.Gtfs.Static;

public class AgencyService : IAgencyService
{
    private readonly GTFSContext _dbContext;
    private readonly ILogger<AgencyService> _logger;
    private readonly IRedisService _redis;

    public AgencyService(GTFSContext dbContext, ILogger<AgencyService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Agency>> GetAllAsync()
    {
        return await _dbContext.Agencies.ToListAsync();
    }

    public async Task<Agency?> GetByIdAsync(string agencyId)
    {
        return await _redis.GetOrSetAsync(
            $"agency-{agencyId}",
            async () => await _dbContext.Agencies.FirstOrDefaultAsync(a => string.Equals(a.AgencyId, agencyId, StringComparison.OrdinalIgnoreCase))
        );
    }

    public async Task<bool> ImportDataAsync(string directoryPath, string? agencyId = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "agency.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return false;
        }

        try
        {
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.BatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingAgencyIds = new(
                await _dbContext.Agencies.Select(a => a.AgencyId.ToLower()).ToListAsync()
            );

            List<Agency> entities = new List<Agency>(batchSize);

            using (StreamReader reader = new StreamReader(filePath))
            {
                string? headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    _logger.LogWarning($"No data found in {filePath}");
                    return false;
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

                    string agencyIdValue = rowData.GetValueOrDefault("agency_id", null) ?? agencyId ?? "";

                    if (existingAgencyIds.Contains(agencyIdValue.ToLower()))
                    {
                        totalIgnored++;
                        continue;
                    }

                    Agency entity = new Agency
                    {
                        Id = Guid.NewGuid().ToString(),
                        AgencyId = agencyIdValue,
                        AgencyName = rowData.GetValueOrDefault("agency_name", "") ?? "",
                        AgencyUrl = rowData.GetValueOrDefault("agency_url", "") ?? "",
                        AgencyTimezone = rowData.GetValueOrDefault("agency_timezone", "") ?? "",
                        AgencyLang = rowData.GetValueOrDefault("agency_lang", "") ?? "",
                        AgencyPhone = rowData.GetValueOrDefault("agency_phone", null),
                        AgencyFareUrl = rowData.GetValueOrDefault("agency_fare_url", null),
                        AgencyEmail = rowData.GetValueOrDefault("agency_email", null)
                    };

                    entities.Add(entity);

                    _logger.LogInformation(
                        $"Importing data to database... ({0})",
                        TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds)
                    );

                    if (entities.Count >= batchSize)
                    {
                        await _dbContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                        existingAgencyIds.Add(agencyIdValue.ToLower());
                    }
                }

                if (entities.Count > 0)
                {
                    await _dbContext.BulkInsertAsync(entities);
                    totalImported += entities.Count;
                    entities.Clear();

                    foreach (Agency e in entities)
                    {
                        existingAgencyIds.Add(e.AgencyId.ToLower());
                    }
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                $"Inserted {totalImported} records from {filePath} in database with {totalIgnored} file(s) ignored. ({0})",
                TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds)
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            return false;
        }
    }
}