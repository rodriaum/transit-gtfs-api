using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using TransitGtfsApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace TransitGtfsApi.Service;

public class TransfersService : ITransfersService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<TransfersService> _logger;
    private readonly IRedisService _redis;

    public TransfersService(TransitDbContext dbContext, ILogger<TransfersService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Transfer>> GetAllAsync()
    {
        return await _dbContext.Transfers.ToListAsync();
    }

    public async Task<List<Transfer>> GetByFromStopIdAsync(string fromStopId)
    {
        return await _redis.GetOrSetAsync(
            $"transfers-from-{fromStopId}",
            async () => await _dbContext.Transfers.Where(t => t.FromStopId == fromStopId).ToListAsync()
        ) ?? new List<Transfer>();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "transfers.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Importing data from {filePath}");
            var entities = new List<Transfer>();
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
                    {
                        rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
                    }
                }

                var entity = new Transfer
                {
                    Id = Guid.NewGuid().ToString(),
                    FromStopId = rowData.GetValueOrDefault("from_stop_id", "") ?? "",
                    ToStopId = rowData.GetValueOrDefault("to_stop_id", "") ?? "",
                    TransferType = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("transfer_type", null)),
                    MinTransferTime = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("min_transfer_time", null), null)
                };
                entities.Add(entity);
            }

            if (entities.Count > 0)
            {
                _dbContext.Transfers.AddRange(entities);
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