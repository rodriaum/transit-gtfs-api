using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class NetworkService : INetworkService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<NetworkService> _logger;
    private readonly IPostgresService _postgresService;

    public NetworkService(GtfsDbContext gtfsDbContext, ILogger<NetworkService> logger, IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<Network>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<Network>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "networks.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Set<Network>().Select(n => n.NetworkId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<Network> entities = new List<Network>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string networkId = rowData.GetValueOrDefault("network_id", "") ?? "";
            string uniqueKey = networkId.ToLower();

            if (existingIds.Contains(uniqueKey))
            {
                totalIgnored++;
                continue;
            }

            Network entity = new Network
            {
                Id = Guid.NewGuid().ToString(),
                NetworkId = networkId,
                Name = rowData.GetValueOrDefault("name", "") ?? "",
                Description = rowData.GetValueOrDefault("description", null)
            };

            entities.Add(entity);
            existingIds.Add(uniqueKey);
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}
