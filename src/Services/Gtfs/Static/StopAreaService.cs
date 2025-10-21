using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class StopAreaService : IStopAreaService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<StopAreaService> _logger;
    private readonly IPostgresService _postgresService;

    public StopAreaService(GtfsDbContext gtfsDbContext, ILogger<StopAreaService> logger, IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<StopArea>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<StopArea>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "stop_areas.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Set<StopArea>().Select(s => s.StopAreaId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<StopArea> entities = new List<StopArea>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string stopAreaId = rowData.GetValueOrDefault("stop_area_id", "") ?? "";
            string uniqueKey = stopAreaId.ToLower();

            if (existingIds.Contains(uniqueKey))
            {
                totalIgnored++;
                continue;
            }

            StopArea entity = new StopArea
            {
                Id = Guid.NewGuid().ToString(),
                StopAreaId = stopAreaId,
                Name = rowData.GetValueOrDefault("name", "") ?? "",
                Description = rowData.GetValueOrDefault("description", null)
            };

            entities.Add(entity);
            existingIds.Add(uniqueKey);
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}