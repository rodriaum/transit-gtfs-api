using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Services.Gtfs.Static;

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

    public async Task<List<Transfer>?> GetByFromStopIdAsync(string fromStopId)
    {
        return await _redis.GetOrSetAsync(
            $"transfers-from-{fromStopId}",
            async () => await _dbContext.Transfers.Where(t => t.FromStopId == fromStopId).ToListAsync()
        ) ?? new List<Transfer>();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "transfers.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Importing data from {filePath}");

            int batchSize = Constant.BatchSizeImport;
            int totalImported = 0;

            List<Transfer> entities = new List<Transfer>(batchSize);

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

                    Transfer entity = new Transfer
                    {
                        Id = Guid.NewGuid().ToString(),
                        FromStopId = rowData.GetValueOrDefault("from_stop_id", "") ?? "",
                        ToStopId = rowData.GetValueOrDefault("to_stop_id", "") ?? "",
                        TransferType = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("transfer_type", null)),
                        MinTransferTime = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("min_transfer_time", null), null)
                    };

                    entities.Add(entity);

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

            string duration = TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"Imported {totalImported} records from {filePath} in {duration}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            throw;
        }
    }
}