using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class TransfersService : ITransfersService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<TransfersService> _logger;
    private readonly IPostgresService _postgresService;

    public TransfersService(GtfsDbContext gtfsDbContext, ILogger<TransfersService> logger, IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<Transfer>> GetAllAsync()
    {
        return await _gtfsDbContext.Transfers.ToListAsync();
    }

    public async Task<List<Transfer>?> GetByFromStopIdAsync(string fromStopId)
    {
        return await _gtfsDbContext.Transfers.Where(t => t.FromStopId == fromStopId).ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "transfers.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Transfers.Select(t => t.FromStopId.ToLower() + ":" + t.ToStopId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<Transfer> entities = new List<Transfer>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string fromStopId = rowData.GetValueOrDefault("from_stop_id", "") ?? "";
            string toStopId = rowData.GetValueOrDefault("to_stop_id", "") ?? "";
            string uniqueKey = fromStopId.ToLower() + ":" + toStopId.ToLower();
            
            if (existingIds.Contains(uniqueKey))
            {
                totalIgnored++;
                continue;
            }

            Transfer entity = new Transfer
            {
                Id = Guid.NewGuid().ToString(),
                FromStopId = fromStopId,
                ToStopId = toStopId,
                TransferType = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("transfer_type", null)),
                MinTransferTime = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("min_transfer_time", null), null)
            };

            entities.Add(entity);
            existingIds.Add(uniqueKey);
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}