using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using Microsoft.EntityFrameworkCore;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Service;

public class AgencyService : IAgencyService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<AgencyService> _logger;
    private readonly IRedisService _redis;

    public AgencyService(TransitDbContext dbContext, ILogger<AgencyService> logger, IRedisService redis)
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
            async () => await _dbContext.Agencies.FirstOrDefaultAsync(a => a.AgencyId == agencyId)
        );
    }

    public async Task ImportDataAsync(string directoryPath, string? agencyKey = null)
    {
        string filePath = Path.Combine(directoryPath, "agency.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Importing data from {filePath}");

            var entities = new List<Agency>();
            string[] lines = await File.ReadAllLinesAsync(filePath);

            if (lines.Length <= 1)
            {
                _logger.LogWarning($"No data found in {filePath}");
                return;
            }

            string[] headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');
                var rowData = new Dictionary<string, string?>();

                for (int j = 0; j < headers.Length; j++)
                {
                    if (j < values.Length)
                        rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
                }

                var entity = new Agency
                {
                    Id = Guid.NewGuid().ToString(),
                    AgencyId = rowData.GetValueOrDefault("agency_id", "") ?? (agencyKey ?? ""),
                    AgencyName = rowData.GetValueOrDefault("agency_name", "") ?? "",
                    AgencyUrl = rowData.GetValueOrDefault("agency_url", "") ?? "",
                    AgencyTimezone = rowData.GetValueOrDefault("agency_timezone", "") ?? "",
                    AgencyLang = rowData.GetValueOrDefault("agency_lang", "") ?? "",
                    AgencyPhone = rowData.GetValueOrDefault("agency_phone", null),
                    AgencyFareUrl = rowData.GetValueOrDefault("agency_fare_url", null),
                    AgencyEmail = rowData.GetValueOrDefault("agency_email", null)
                };

                entities.Add(entity);
            }

            if (entities.Count > 0)
            {
                _dbContext.Agencies.AddRange(entities);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Imported {entities.Count} records from {filePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            throw;
        }
    }
}