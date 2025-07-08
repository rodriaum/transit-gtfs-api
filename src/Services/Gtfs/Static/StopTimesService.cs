using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Interfaces.Gtfs.Realtime;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;
using TransitGtfsApi.Utils;
using TransitRealtime;

namespace TransitGtfsApi.Services.Gtfs.Static;

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

    public async Task<List<StopTime>> GetAllAsync(int page = 1, int pageSize = 100)
    {
        int skip = (page - 1) * pageSize;
        return await _dbContext.StopTimes.Skip(skip).Take(pageSize).ToListAsync();
    }

    public async Task<List<StopTime>?> GetByTripIdAsync(string tripId, bool realtime = false)
    {
        var stopTimes = await _redis.GetOrSetAsync(
            $"stop-times-trip-{tripId}",
            async () => await _dbContext.StopTimes
                .Where(st => st.TripId == tripId)
                .OrderBy(st => st.StopSequence)
                .ToListAsync()
        );

        if (!realtime || stopTimes == null || !stopTimes.Any())
            return stopTimes;

        return await EnrichWithRealtimeData(stopTimes, tripId);
    }

    public async Task<List<StopTime>?> GetByStopIdAsync(string stopId, int page = 1, int pageSize = 100, bool realtime = false)
    {
        var stopTimes = await _redis.GetOrSetAsync(
            $"stop-times-stop-{stopId}-{page}-{pageSize}",
            async () =>
            {
                var skip = (page - 1) * pageSize;
                return await _dbContext.StopTimes
                    .Where(st => st.StopId == stopId)
                    .OrderBy(st => st.ArrivalTime)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();
            }
        ) ?? new List<StopTime>();

        if (!realtime || !stopTimes.Any())
            return stopTimes;

        return await EnrichWithRealtimeDataByStop(stopTimes, stopId);
    }

    private async Task<List<StopTime>?> EnrichWithRealtimeData(List<StopTime> stopTimes, string tripId)
    {
        try
        {
            // Buscar atualizações de viagem em tempo real
            var agencies = await GetAgenciesForTrip(tripId);

            foreach (var agencyId in agencies)
            {
                var tripUpdates = await _realtimeService.GetTripUpdatesAsync(
                    agencyId: agencyId,
                    tripId: tripId
                );

                if (tripUpdates?.Any() == true)
                {
                    var tripUpdate = tripUpdates.First();
                    ApplyTripUpdates(stopTimes, tripUpdate);
                }

                // Buscar posições de veículos para cálculo de ETA
                var vehiclePositions = await _realtimeService.GetVehiclePositionsAsync(
                    agencyId: agencyId,
                    tripId: tripId
                );

                if (vehiclePositions?.Any() == true)
                {
                    var vehiclePosition = vehiclePositions.First();
                    await CalculateRealtimeArrivals(stopTimes, vehiclePosition, tripId);
                }
            }

            return stopTimes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching stop times with realtime data for trip {TripId}", tripId);
            return stopTimes;
        }
    }

    private async Task CalculateRealtimeArrivals(List<StopTime> stopTimes, VehiclePosition vehiclePosition, string tripId)
    {
        // Verifica se há posição válida do veículo
        if (vehiclePosition?.Position == null)
            return;

        double vehicleLat = vehiclePosition.Position.Latitude;
        double vehicleLon = vehiclePosition.Position.Longitude;

        // Busca todas as paradas do banco de dados para este trip
        var stopIds = stopTimes.Select(st => st.StopId).Distinct().ToList();
        var stops = await _dbContext.Stops
            .Where(s => stopIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        // Estima velocidade média do veículo
        double avgSpeed = EstimateAverageSpeed(vehiclePosition); // metros/segundo

        DateTime now = DateTime.UtcNow;

        foreach (var stopTime in stopTimes)
        {
            if (!stops.TryGetValue(stopTime.StopId, out var stop))
                continue;

            // Obtém coordenadas da parada
            double stopLat = stop.StopLat;
            double stopLon = stop.StopLon;

            // Calcula distância do veículo até a parada
            double distance = CalculateDistance(vehicleLat, vehicleLon, stopLat, stopLon); // metros

            // Estima tempo de chegada em segundos
            double etaSeconds = distance / avgSpeed;

            // Define o horário estimado de chegada
            stopTime.RealtimeArrival = now.AddSeconds(etaSeconds);
            stopTime.IsRealtime = true;
            stopTime.LastRealtimeUpdate = now;
            stopTime.RealtimeStatus = "approaching";
        }
    }

    private async Task<List<StopTime>?> EnrichWithRealtimeDataByStop(List<StopTime> stopTimes, string stopId)
    {
        try
        {
            var agencies = await GetAgenciesForStop(stopId);

            foreach (var agencyId in agencies)
            {
                // Buscar atualizações para todas as viagens que passam por esta parada
                var tripIds = stopTimes.Select(st => st.TripId).Distinct();

                foreach (var tripId in tripIds)
                {
                    var tripUpdates = await _realtimeService.GetTripUpdatesAsync(
                        agencyId: agencyId,
                        tripId: tripId,
                        stopId: stopId
                    );

                    if (tripUpdates?.Any() == true)
                    {
                        var tripUpdate = tripUpdates.First();
                        var relevantStopTimes = stopTimes.Where(st => st.TripId == tripId).ToList();
                        ApplyTripUpdates(relevantStopTimes, tripUpdate);
                    }

                    // Buscar posições de veículos
                    var vehiclePositions = await _realtimeService.GetVehiclePositionsAsync(
                        agencyId: agencyId,
                        tripId: tripId
                    );

                    if (vehiclePositions?.Any() == true)
                    {
                        var vehiclePosition = vehiclePositions.First();
                        var relevantStopTimes = stopTimes.Where(st => st.TripId == tripId).ToList();
                        await CalculateRealtimeArrivals(relevantStopTimes, vehiclePosition, tripId);
                    }
                }
            }

            return stopTimes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching stop times with realtime data for stop {StopId}", stopId);
            return stopTimes;
        }
    }

    private void ApplyTripUpdates(List<StopTime> stopTimes, TripUpdate tripUpdate)
    {
        foreach (var stopTimeUpdate in tripUpdate.StopTimeUpdate)
        {
            var stopTime = stopTimes.FirstOrDefault(st =>
                st.StopId == stopTimeUpdate.StopId &&
                st.StopSequence == stopTimeUpdate.StopSequence);

            if (stopTime != null)
            {
                // Aplicar atraso/adiantamento
                if (stopTimeUpdate.Arrival?.HasDelay == true)
                {
                    var originalArrival = ParseTimeToSeconds(stopTime.ArrivalTime);
                    var delaySeconds = stopTimeUpdate.Arrival.Delay;
                    var newArrival = originalArrival + delaySeconds;
                    stopTime.ArrivalTime = FormatSecondsToTime(newArrival);
                    stopTime.RealtimeArrival = DateTimeOffset.FromUnixTimeSeconds(stopTimeUpdate.Arrival.Time).DateTime;
                }

                if (stopTimeUpdate.Departure?.HasDelay == true)
                {
                    var originalDeparture = ParseTimeToSeconds(stopTime.DepartureTime);
                    var delaySeconds = stopTimeUpdate.Departure.Delay;
                    var newDeparture = originalDeparture + delaySeconds;
                    stopTime.DepartureTime = FormatSecondsToTime(newDeparture);
                    stopTime.RealtimeDeparture = DateTimeOffset.FromUnixTimeSeconds(stopTimeUpdate.Departure.Time).DateTime;
                }
            }
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Fórmula de Haversine para calcular distância em metros
        const double R = 6371000; // Raio da Terra em metros

        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                Math.Cos(φ1) * Math.Cos(φ2) *
                Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private double EstimateAverageSpeed(VehiclePosition vehiclePosition)
    {
        // Estimar velocidade média baseada no tipo de veículo/rota
        // Valores em m/s
        return 8.33; // ~30 km/h como padrão para transporte público urbano
    }

    private async Task<List<string>> GetAgenciesForTrip(string tripId)
    {
        // Buscar agências associadas ao trip
        var agencies = await _dbContext.Trips
            .Where(t => t.Id == tripId)
            .Join(_dbContext.Routes, t => t.RouteId, r => r.Id, (t, r) => r.AgencyId)
            .Distinct()
            .ToListAsync();

        return agencies.Where(a => !string.IsNullOrEmpty(a)).ToList();
    }

    private async Task<List<string>> GetAgenciesForStop(string stopId)
    {
        // Buscar agências que operam nesta parada
        var agencies = await _dbContext.StopTimes
            .Where(st => st.StopId == stopId)
            .Join(_dbContext.Trips, st => st.TripId, t => t.Id, (st, t) => t)
            .Join(_dbContext.Routes, t => t.RouteId, r => r.Id, (t, r) => r.AgencyId)
            .Distinct()
            .ToListAsync();

        return agencies.Where(a => !string.IsNullOrEmpty(a)).ToList();
    }

    private int ParseTimeToSeconds(string time)
    {
        if (string.IsNullOrEmpty(time)) return 0;

        var parts = time.Split(':');
        if (parts.Length != 3) return 0;

        if (int.TryParse(parts[0], out int hours) &&
            int.TryParse(parts[1], out int minutes) &&
            int.TryParse(parts[2], out int seconds))
        {
            return hours * 3600 + minutes * 60 + seconds;
        }

        return 0;
    }

    private string FormatSecondsToTime(int totalSeconds)
    {
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;

        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
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