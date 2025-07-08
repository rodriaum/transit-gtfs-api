using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Services.Gtfs.Static;

public class StopTimesService : IStopTimesService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<StopTimesService> _logger;
    private readonly IRedisService _redis;

    public StopTimesService(TransitDbContext dbContext, ILogger<StopTimesService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<StopTime>> GetAllAsync(int page = 1, int pageSize = 100)
    {
        int skip = (page - 1) * pageSize;
        return await _dbContext.StopTimes.Skip(skip).Take(pageSize).ToListAsync();
    }

    public async Task<List<StopTime>?> GetByTripIdAsync(string tripId)
    {
        return await _redis.GetOrSetAsync(
            $"stop-times-trip-{tripId}",
            async () => await _dbContext.StopTimes.Where(st => st.TripId == tripId).ToListAsync()
        );
    }

    public async Task<List<StopTime>?> GetByStopIdAsync(string stopId, int page = 1, int pageSize = 100)
    {
        return await _redis.GetOrSetAsync(
            $"stop-times-stop-{stopId}-{page}-{pageSize}",
            async () =>
            {
                var skip = (page - 1) * pageSize;
                return await _dbContext.StopTimes.Where(st => st.StopId == stopId)
                                                .Skip(skip)
                                                .Take(pageSize)
                                                .ToListAsync();
            }
        ) ?? new List<StopTime>();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "stop_times.txt");

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
                await _dbContext.StopTimes.Select(st => st.TripId.ToLower() + ":" + st.StopId.ToLower() + ":" + st.StopSequence.ToString()).ToListAsync()
            );

            List<StopTime> entities = new List<StopTime>(batchSize);

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

                    string tripId = rowData.GetValueOrDefault("trip_id", "") ?? "";
                    string stopId = rowData.GetValueOrDefault("stop_id", "") ?? "";
                    int stopSequence = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("stop_sequence", null));
                    string uniqueKey = tripId.ToLower() + ":" + stopId.ToLower() + ":" + stopSequence.ToString();
                    if (existingIds.Contains(uniqueKey))
                    {
                        totalIgnored++;
                        continue;
                    }

                    int pickupTypeId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("pickup_type", null), -1);
                    int dropOffTypeId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("drop_off_type", null), -1);
                    int timepointId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("timepoint", null), -1);

                    StopTime entity = new StopTime
                    {
                        Id = Guid.NewGuid().ToString(),
                        TripId = tripId,
                        ArrivalTime = rowData.GetValueOrDefault("arrival_time", "") ?? "",
                        DepartureTime = rowData.GetValueOrDefault("departure_time", "") ?? "",
                        StopId = stopId,
                        StopSequence = stopSequence,
                        StopHeadsign = rowData.GetValueOrDefault("stop_headsign", null),
                        PickupType = pickupTypeId != -1 ? EnumUtil.FromValue<PickupType>(pickupTypeId) : null,
                        DropOffType = dropOffTypeId != -1 ? dropOffTypeId : null,
                        ShapeDistTraveled = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_dist_traveled", null), format: CultureInfo.InvariantCulture),
                        Timepoint = timepointId != -1 ? EnumUtil.FromValue<TimepointType>(timepointId) : null
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
                $"Inserted {totalImported} records from {filePath} in database with {totalIgnored} file(s) ignored. ({TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds)})"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            return;
        }
    }
}