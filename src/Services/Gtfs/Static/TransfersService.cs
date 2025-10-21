using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
    private readonly IRedisService _redis;

    public TransfersService(GtfsDbContext gtfsDbContext, ILogger<TransfersService> logger, IRedisService redis)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Transfer>> GetAllAsync()
    {
        return await _gtfsDbContext.Transfers.ToListAsync();
    }

    public async Task<List<Transfer>?> GetByFromStopIdAsync(string fromStopId)
    {
        return await _redis.GetOrSetAsync(
            $"transfers-from-{fromStopId}",
            async () => await _gtfsDbContext.Transfers.Where(t => t.FromStopId == fromStopId).ToListAsync()
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
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.SqlBatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingIds = new HashSet<string>(
                await _gtfsDbContext.Transfers.Select(t => t.FromStopId.ToLower() + ":" + t.ToStopId.ToLower()).ToListAsync()
            );

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

                    if (entities.Count >= batchSize)
                    {
                        await _gtfsDbContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _gtfsDbContext.BulkInsertAsync(entities);
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Inserted {0} records from {1} in database with {2} line(s) ignored. ({3})",
                totalImported,
                filePath,
                totalIgnored,
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