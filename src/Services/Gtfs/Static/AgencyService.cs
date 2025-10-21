using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class AgencyService : IAgencyService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<AgencyService> _logger;
    private readonly IPostgresService _postgresService;

    public AgencyService(GtfsDbContext gtfsDbContext, ILogger<AgencyService> logger, IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<Agency>> GetAllAsync()
    {
        return await _gtfsDbContext.Agencies.ToListAsync();
    }

    public async Task<Agency?> GetByIdAsync(string agencyId)
    {
        return await _gtfsDbContext.Agencies.FirstOrDefaultAsync(a =>
            string.Equals(a.AgencyId, agencyId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> ImportDataAsync(string directoryPath, string agencyId)
    {
        string filePath = Path.Combine(directoryPath, "agency.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Agencies.Select(a => a.AgencyId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvImportUtil.ReadCsvAsync(filePath, _logger);
        List<Agency> entities = new List<Agency>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            if (existingIds.Contains(agencyId.ToLower()))
            {
                _logger.LogInformation("Agency data {0} already exists. It will be ignored!", agencyId);
                totalIgnored++;
                continue;
            }

            Agency entity = new Agency
            {
                Id = Guid.NewGuid().ToString(),
                AgencyId = agencyId,
                AgencyName = rowData.GetValueOrDefault("agency_name", "") ?? "",
                AgencyUrl = rowData.GetValueOrDefault("agency_url", "") ?? "",
                AgencyTimezone = rowData.GetValueOrDefault("agency_timezone", "") ?? "",
                AgencyLang = rowData.GetValueOrDefault("agency_lang", "") ?? "",
                AgencyPhone = rowData.GetValueOrDefault("agency_phone", null),
                AgencyFareUrl = rowData.GetValueOrDefault("agency_fare_url", null),
                AgencyEmail = rowData.GetValueOrDefault("agency_email", null)
            };

            entities.Add(entity);
            existingIds.Add(agencyId.ToLower());
        }

        int totalImported = await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);

        return totalImported > 0;
    }
}