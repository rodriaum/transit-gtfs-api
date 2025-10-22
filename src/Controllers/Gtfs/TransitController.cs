using Microsoft.AspNetCore.Mvc;
using TransitRealtime;
using Tranzor.DTOs;
using Tranzor.Interfaces.Gtfs;
using Tranzor.Interfaces.Gtfs.Realtime;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Models.OTP;
using Tranzor.Utils;
using Route = Tranzor.Models.Route;
using Stop = Tranzor.Models.Stop;

namespace Tranzor.Controllers.Gtfs;

[ApiController]
[Route("api/v1/tranzor")]
public class TransitController : ControllerBase
{
    private readonly IRoutesService _routesService;
    private readonly ITripsService _tripsService;
    private readonly IStopTimesService _stopTimesService;
    private readonly IStopsService _stopsService;
    private readonly IGtfsRealtimeCacheService _gtfsRealtimeService;
    private readonly IOpenTripPlannerService _routerService;
    private readonly ILogger<TransitController> _logger;

    public TransitController(
        IRoutesService routesService,
        ITripsService tripsService,
        IStopTimesService stopTimesService,
        IStopsService stopsService,
        IGtfsRealtimeCacheService gtfsRealtimeService,
        IOpenTripPlannerService routerService,
        ILogger<TransitController> logger)
    {
        _routesService = routesService;
        _tripsService = tripsService;
        _stopTimesService = stopTimesService;
        _stopsService = stopsService;
        _gtfsRealtimeService = gtfsRealtimeService;
        _routerService = routerService;
        _logger = logger;
    }

    [HttpGet("route-with-trips/{routeId}")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "routeId" })] // Cache 5 minutos
    public async Task<ActionResult<RouteWithTripsDto>> GetRouteWithTrips(string routeId)
    {
        try
        {
            Route? route = await _routesService.GetByIdAsync(routeId);

            if (route == null)
                return NotFound(new { message = $"Route with ID {routeId} not found" });

            List<Trip>? trips = await _tripsService.GetByRouteIdAsync(routeId);

            if (trips == null || trips.Count == 0)
                return NotFound(new { message = $"No trips found for route with ID {routeId}" });

            return new RouteWithTripsDto
            {
                Route = route,
                Trips = trips
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching route with trips: {routeId}");
            return StatusCode(500, new { message = "Error processing request", error = ex.Message });
        }
    }

    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "tripId" })]
    [HttpGet("trip-with-stops/{tripId}")]
    public async Task<ActionResult<TripWithStopTimesDto>> GetTripWithStops(string tripId)
    {
        try
        {
            Trip? trip = await _tripsService.GetByIdAsync(tripId);

            if (trip == null)
                return NotFound(new { message = $"Trip with ID {tripId} not found" });

            List<StopTime>? stopTimes = await _stopTimesService.GetByTripIdAsync(tripId);

            if (stopTimes == null || !stopTimes.Any())
                return NotFound(new { message = $"No stop times found for trip {tripId}" });

            List<UpcomingDeparturesDto> stopTimesWithStops = new();

            foreach (var stopTime in stopTimes.OrderBy(st => st.StopSequence))
            {
                Stop? stop = await _stopsService.GetByIdAsync(stopTime.StopId);
                if (stop == null) continue;

                stopTimesWithStops.Add(new UpcomingDeparturesDto
                {
                    StopTime = stopTime,
                    Stop = stop
                });
            }

            return new TripWithStopTimesDto
            {
                Trip = trip,
                StopTimes = stopTimesWithStops
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching for trip with stops:{tripId}");
            return StatusCode(500, new { message = "Error processing request", error = ex.Message });
        }
    }

    [HttpGet("upcoming-departures/{stopId}")]
    public async Task<ActionResult<List<UpcomingDeparturesDto>>> GetUpcomingDepartures(
        string stopId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 3,
        [FromQuery] DateTime? referenceTime = null)
    {
        try
        {
            Stop? stop = await _stopsService.GetByIdAsync(stopId);

            if (stop == null)
                return NotFound(new { message = $"Stop with ID {stopId} not found" });

            List<StopTime>? upcomingDepartures =
                await _stopTimesService.GetUpcomingDeparturesByStopIdAsync(stopId, page: page, pageSize: pageSize, referenceTime: referenceTime);

            if (upcomingDepartures == null || upcomingDepartures.Count == 0)
                return NotFound(new { message = $"No stop times found for stop {stopId}" });

            List<TripUpdate>? tripUpdates =
                await _gtfsRealtimeService.GetTripUpdatesAsync(stopId: stopId);
            List<VehiclePosition>? vehiclePositions =
                await _gtfsRealtimeService.GetVehiclePositionsAsync(stopId: stopId);

            Dictionary<string, TripUpdate> tripUpdateDict = tripUpdates?
                .Where(tu => tu?.Trip?.TripId != null)
                .ToDictionary(tu => tu.Trip.TripId!) ?? new();

            Dictionary<string, VehiclePosition> vehicleDict = vehiclePositions?
                .Where(v => v?.Trip?.TripId != null && v.Position != null)
                .GroupBy(v => v.Trip.TripId!)
                .ToDictionary(g => g.Key, g => g.First()) ?? new();

            List<string> tripIds = upcomingDepartures.Select(st => st.TripId)
                .Distinct()
                .ToList();

            List<Trip>? tripsList = await _tripsService.GetTripsBatchAsync(tripIds);

            if (tripsList == null || !tripsList.Any())
                return NotFound(new { message = "No trip with one or more of the indicated ids was found." });

            Dictionary<string, Trip> tripsDict = tripsList.ToDictionary(t => t.TripId);

            List<UpcomingDeparturesDto> result = new();

            foreach (StopTime departure in upcomingDepartures)
            {
                bool hasRealtime = false;

                // (1) Use TripUpdate if available
                if (tripUpdateDict.TryGetValue(departure.TripId, out var tripUpdate))
                {
                    var stopUpdate = tripUpdate.StopTimeUpdate
                        .FirstOrDefault(s => s.StopId == stopId);

                    if (stopUpdate != null)
                    {
                        if (stopUpdate.Arrival?.Time != null)
                            departure.RealtimeArrival = DateTimeOffset.FromUnixTimeSeconds(stopUpdate.Arrival.Time)
                                .LocalDateTime;

                        if (stopUpdate.Departure?.Time != null)
                            departure.RealtimeDeparture = DateTimeOffset.FromUnixTimeSeconds(stopUpdate.Departure.Time)
                                .LocalDateTime;

                        departure.DelaySeconds = stopUpdate.Arrival?.Delay ?? stopUpdate.Departure?.Delay ?? 0;
                        departure.IsRealtime = true;
                        departure.LastRealtimeUpdate = DateTime.Now;
                        hasRealtime = true;
                    }
                }

                // (2) If there is no TripUpdate, use estimation via VehiclePosition
                if (!hasRealtime && vehicleDict.TryGetValue(departure.TripId, out var vehicle))
                {
                    if (stop.StopLat != 0 && stop.StopLon != 0)
                    {
                        double distance = MathUtil.Haversine(
                            stop.StopLat,
                            stop.StopLon,
                            vehicle.Position.Latitude,
                            vehicle.Position.Longitude
                        );

                        distance = !NumberUtil.isValidNumber(distance) ? 0 : distance;
                        double speed = vehicle.Position.Speed;
                        speed = !NumberUtil.isValidNumber(speed) ? 0 : speed;

                        departure.DistanceToStop = distance;
                        departure.VehicleSpeed = speed;

                        if (speed > 1)
                        {
                            double seconds = distance / speed;

                            if (!double.IsInfinity(seconds) && !double.IsNaN(seconds))
                            {
                                departure.RealtimeArrival = DateTime.Now.AddSeconds(seconds);
                                departure.RealtimeDeparture = departure.RealtimeArrival;
                                departure.IsRealtime = true;
                                departure.LastRealtimeUpdate = DateTime.Now;
                            }
                        }
                    }
                }

                Trip? trip = tripsDict.TryGetValue(departure.TripId, out var t) ? t : null;
                Route? route = trip != null ? await _routesService.GetByIdAsync(trip.RouteId) : null;

                result.Add(new UpcomingDeparturesDto
                {
                    StopTime = departure,
                    Stop = stop,
                    Trip = trip,
                    Route = route
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching for next departures for stop: {stopId}");
            return StatusCode(500, new { message = "Error processing request", error = ex.Message });
        }
    }

    [HttpGet("plan-router")]
    public async Task<ActionResult<List<RoutePlan>>> PlanRouter(
        [FromQuery] double fromLat,
        [FromQuery] double fromLon,
        [FromQuery] double toLat,
        [FromQuery] double toLon,
        [FromQuery] DateTime? departureTime,
        [FromQuery] int maxRoutes = 1)
    {
        try
        {
            if (departureTime == null)
                departureTime = DateTime.Now;

            List<RoutePlan> routePlan =
                await _routerService.PlanRouteAsync(fromLat, fromLon, toLat, toLon, departureTime.Value, maxRoutes);

            if (routePlan == null)
                return NotFound();

            return routePlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing a route, is OpenTripPlanner running?");
            return StatusCode(500,
                new
                {
                    message = "Error processing order, check if you have OpenTripPlanner running.", error = ex.Message
                });
        }
    }
}