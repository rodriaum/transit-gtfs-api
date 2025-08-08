using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Globalization;
using Tranzor.Databases;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Realtime;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class StopTimesService : IStopTimesService
{
    private readonly GTFSContext _dbContext;
    private readonly ILogger<StopTimesService> _logger;
    private readonly IRedisService _redis;
    private readonly IGtfsRealtimeCacheService _realtimeService;

    public StopTimesService(
        GTFSContext dbContext,
        ILogger<StopTimesService> logger,
        IRedisService redis,
        IGtfsRealtimeCacheService realtimeService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
        _realtimeService = realtimeService;
    }

    private IQueryable<string> GetActiveServiceIds(DateTime date)
    {
        DateOnly dateOnly = DateOnly.FromDateTime(date);
        string dayOfWeek = date.DayOfWeek.ToString().ToLower();
        string dayColumn = char.ToUpper(dayOfWeek[0]) + dayOfWeek.Substring(1);

        string sqlDate = date.ToString("yyyy-MM-dd");

        IQueryable<string> calendarQuery = _dbContext.Calendars
            .Where(c => EF.Functions.ToDate(EF.Property<string>(c, "StartDate"), "YYYYMMDD") <= dateOnly &&
                        EF.Functions.ToDate(EF.Property<string>(c, "EndDate"), "YYYYMMDD") >= dateOnly &&
                        EF.Property<int>(c, dayColumn) == (int)StatusType.Active)
            .Select(c => c.ServiceId);

        return calendarQuery
            .Union(_dbContext.CalendarDates
                .Where(cd => EF.Functions.ToDate(cd.Date, "YYYYMMDD") == dateOnly && cd.ExceptionType == ExceptionType.Added)
                .Select(cd => cd.ServiceId))
            .Except(_dbContext.CalendarDates
                .Where(cd => EF.Functions.ToDate(cd.Date, "YYYYMMDD") == dateOnly && cd.ExceptionType == ExceptionType.Removed)
                .Select(cd => cd.ServiceId));
    }

    public async Task<List<StopTime>> GetAllAsync(int page = 1, int pageSize = 100)
    {
        int skip = (page - 1) * pageSize;
        return await _dbContext.StopTimes.Skip(skip).Take(pageSize).ToListAsync();
    }

    public async Task<List<StopTime>?> GetByTripIdAsync(string tripId, bool ignoreCalendar = false)
    {
        DateTime date = DateTime.Now;

        string keySource = $"stop-times-trip-{tripId}-{ignoreCalendar}-{date.ToString("yyyyMMdd")}";
        string cacheKey = StringUtils.GenerateHash(keySource);

        return await _redis.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                IQueryable<StopTime> query = _dbContext.StopTimes.Where(st => st.TripId == tripId);

                if (!ignoreCalendar)
                {
                    IQueryable<string> activeServiceIds = GetActiveServiceIds(date);

                    query = from st in query
                            join trip in _dbContext.Trips on st.TripId equals trip.TripId
                            where activeServiceIds.Contains(trip.ServiceId)
                            select st;
                }

                return await query
                    .OrderBy(st => st.StopSequence)
                    .ToListAsync();
            }
        );
    }

    public async Task<List<StopTime>?> GetByStopIdAsync(string stopId, int page = 1, int pageSize = 100, bool ignoreCalendar = false)
    {
        DateTime date = DateTime.Now;

        string keySource = $"stop-times-stop-{stopId}-{page}-{pageSize}-{ignoreCalendar}-{date.ToString("yyyyMMdd")}";
        string cacheKey = StringUtils.GenerateHash(keySource);

        return await _redis.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                IQueryable<StopTime> query = _dbContext.StopTimes.Where(st => st.StopId == stopId);

                if (!ignoreCalendar)
                {
                    IQueryable<string> activeServiceIds = GetActiveServiceIds(date);

                    query = from st in query
                            join trip in _dbContext.Trips on st.TripId equals trip.TripId
                            where activeServiceIds.Contains(trip.ServiceId)
                            select st;
                }

                return await query
                    .OrderBy(st => st.ArrivalTime)
                    .Skip((page - 1) * pageSize)
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
                $"Inserted {totalImported} records from {filePath} in database with {totalIgnored} line(s) ignored. ({TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds)})"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            return;
        }
    }
}