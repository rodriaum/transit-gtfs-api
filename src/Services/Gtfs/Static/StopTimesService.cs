using Cassandra;
using System.Diagnostics;
using System.Globalization;
using Tranzor.Context;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Realtime;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;
using Microsoft.EntityFrameworkCore;

namespace Tranzor.Services.Gtfs.Static;

public class StopTimesService : IStopTimesService
{
    private readonly GtfsDbContext _gtfsDBContext;
    private readonly ICassandraService _cassandraService;
    private readonly ILogger<StopTimesService> _logger;
    private readonly IRedisService _redis;
    private readonly IGtfsRealtimeCacheService _realtimeService;

    public StopTimesService(
        GtfsDbContext gtfsDBContext,
        ICassandraService cassandraService,
        ILogger<StopTimesService> logger,
        IRedisService redis,
        IGtfsRealtimeCacheService realtimeService)
    {
        _gtfsDBContext = gtfsDBContext;
        _cassandraService = cassandraService;
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

        IQueryable<string> calendarQuery = _gtfsDBContext.Calendars
            .Where(c => EF.Functions.ToDate(EF.Property<string>(c, "StartDate"), "YYYYMMDD") <= dateOnly &&
                        EF.Functions.ToDate(EF.Property<string>(c, "EndDate"), "YYYYMMDD") >= dateOnly &&
                        EF.Property<int>(c, dayColumn) == (int)StatusType.Active)
            .Select(c => c.ServiceId);

        return calendarQuery
            .Union(_gtfsDBContext.CalendarDates
                .Where(cd => EF.Functions.ToDate(cd.Date, "YYYYMMDD") == dateOnly && cd.ExceptionType == ExceptionType.Added)
                .Select(cd => cd.ServiceId))
            .Except(_gtfsDBContext.CalendarDates
                .Where(cd => EF.Functions.ToDate(cd.Date, "YYYYMMDD") == dateOnly && cd.ExceptionType == ExceptionType.Removed)
                .Select(cd => cd.ServiceId));
    }

    public async Task<List<StopTime>> GetAllAsync(int page = 1, int pageSize = 100)
    {
        Cassandra.ISession session = await _cassandraService.GetSessionAsync();
        
        string query = "SELECT * FROM stop_times LIMIT ?";
        PreparedStatement prepared = await session.PrepareAsync(query);
        BoundStatement bound = prepared.Bind(pageSize);

        RowSet rowSet = await session.ExecuteAsync(bound);
        List<StopTime> stopTimes = new List<StopTime>();

        foreach (Row row in rowSet)
        {
            stopTimes.Add(MapRowToStopTime(row));
        }

        return stopTimes;
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
                Cassandra.ISession session = await _cassandraService.GetSessionAsync();

                if (!ignoreCalendar)
                {
                    List<string> activeServiceIds = await GetActiveServiceIds(date).ToListAsync();

                    Trip? trip = await _gtfsDBContext.Trips
                        .Where(t => t.TripId == tripId && activeServiceIds.Contains(t.ServiceId))
                        .FirstOrDefaultAsync();

                    if (trip == null)
                    {
                        return new List<StopTime>();
                    }
                }

                string query = "SELECT * FROM stop_times WHERE trip_id = ? ORDER BY stop_sequence ASC";
                PreparedStatement prepared = await session.PrepareAsync(query);
                BoundStatement bound = prepared.Bind(tripId);

                RowSet rowSet = await session.ExecuteAsync(bound);
                List<StopTime> stopTimes = new List<StopTime>();

                foreach (Row row in rowSet)
                {
                    stopTimes.Add(MapRowToStopTime(row));
                }

                return stopTimes;
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
                Cassandra.ISession session = await _cassandraService.GetSessionAsync();

                string query = "SELECT * FROM stop_times WHERE stop_id = ? ALLOW FILTERING";
                PreparedStatement prepared = await session.PrepareAsync(query);
                BoundStatement bound = prepared.Bind(stopId);

                RowSet rowSet = await session.ExecuteAsync(bound);
                List<StopTime> allStopTimes = new List<StopTime>();

                foreach (Row row in rowSet)
                {
                    allStopTimes.Add(MapRowToStopTime(row));
                }

                if (!ignoreCalendar)
                {
                    List<string> activeServiceIds = await GetActiveServiceIds(date).ToListAsync();
                    List<string> activeTripIds = await _gtfsDBContext.Trips
                        .Where(t => activeServiceIds.Contains(t.ServiceId))
                        .Select(t => t.TripId)
                        .ToListAsync();

                    allStopTimes = allStopTimes
                        .Where(st => activeTripIds.Contains(st.TripId))
                        .ToList();
                }

                return allStopTimes
                    .OrderBy(st => TimeFormatUtil.ParseGtfsTime(st.ArrivalTime))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
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

            Cassandra.ISession session = await _cassandraService.GetSessionAsync();

            int batchSize = Constant.CassandraBatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            string checkQuery = "SELECT trip_id, stop_sequence FROM stop_times WHERE trip_id = ? AND stop_sequence = ?";
            PreparedStatement checkPrepared = await session.PrepareAsync(checkQuery);

            string insertQuery = @"
                INSERT INTO stop_times (
                    id, trip_id, arrival_time, departure_time, stop_id, stop_sequence,
                    stop_headsign, pickup_type, drop_off_type, shape_dist_traveled, timepoint
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
            ";
            PreparedStatement insertPrepared = await session.PrepareAsync(insertQuery);

            BatchStatement batch = new BatchStatement();
            int batchCount = 0;

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
                    var rowData = new Dictionary<string, string?>();

                    for (int j = 0; j < headers.Length; j++)
                    {
                        if (j < values.Length)
                        {
                            rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
                        }
                    }

                    string tripId = rowData.GetValueOrDefault("trip_id", "") ?? "";
                    int stopSequence = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("stop_sequence", null));

                    BoundStatement checkBound = checkPrepared.Bind(tripId, stopSequence);
                    RowSet existingRows = await session.ExecuteAsync(checkBound);

                    if (existingRows.Any())
                    {
                        totalIgnored++;
                        continue;
                    }

                    int pickupTypeId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("pickup_type", null), -1);
                    int dropOffTypeId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("drop_off_type", null), -1);
                    int timepointId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("timepoint", null), -1);

                    BoundStatement insertBound = insertPrepared.Bind(
                        Guid.NewGuid().ToString(),
                        tripId,
                        rowData.GetValueOrDefault("arrival_time", "") ?? "",
                        rowData.GetValueOrDefault("departure_time", "") ?? "",
                        rowData.GetValueOrDefault("stop_id", "") ?? "",
                        stopSequence,
                        rowData.GetValueOrDefault("stop_headsign", null),
                        pickupTypeId != -1 ? pickupTypeId : (int?)null,
                        dropOffTypeId != -1 ? dropOffTypeId : (int?)null,
                        NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_dist_traveled", null), format: CultureInfo.InvariantCulture),
                        timepointId != -1 ? timepointId : (int?)null
                    );

                    batch.Add(insertBound);
                    batchCount++;

                    if (batchCount >= batchSize)
                    {
                        await session.ExecuteAsync(batch);
                        totalImported += batchCount;
                        batch = new BatchStatement();
                        batchCount = 0;
                    }
                }

                if (batchCount > 0)
                {
                    await session.ExecuteAsync(batch);
                    totalImported += batchCount;
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Inserted {0} records from {1} in cassandra with {2} line(s) ignored. ({3})",
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

    private StopTime MapRowToStopTime(Row row)
    {
        int? pickupTypeValue = row.IsNull("pickup_type") ? null : row.GetValue<int?>("pickup_type");
        int? timepointValue = row.IsNull("timepoint") ? null : row.GetValue<int?>("timepoint");

        return new StopTime
        {
            Id = row.GetValue<string>("id"),
            TripId = row.GetValue<string>("trip_id"),
            ArrivalTime = row.GetValue<string>("arrival_time"),
            DepartureTime = row.GetValue<string>("departure_time"),
            StopId = row.GetValue<string>("stop_id"),
            StopSequence = row.GetValue<int>("stop_sequence"),
            StopHeadsign = row.IsNull("stop_headsign") ? null : row.GetValue<string>("stop_headsign"),
            PickupType = pickupTypeValue.HasValue ? EnumUtil.FromValue<PickupType>(pickupTypeValue.Value) : null,
            DropOffType = row.IsNull("drop_off_type") ? null : row.GetValue<int?>("drop_off_type"),
            ShapeDistTraveled = row.IsNull("shape_dist_traveled") ? null : row.GetValue<double?>("shape_dist_traveled"),
            Timepoint = timepointValue.HasValue ? EnumUtil.FromValue<TimepointType>(timepointValue.Value) : null
        };
    }
}