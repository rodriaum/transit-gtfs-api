using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Models;
using Microsoft.AspNetCore.Mvc;
using TransitGtfsApi.DTOs;

namespace TransitGtfsApi.Controllers.Gtfs;

[ApiController]
[Route("api/v1/transit/gtfs")]
public class MetroController : ControllerBase
{
    private readonly IRoutesService _routesService;
    private readonly ITripsService _tripsService;
    private readonly IStopTimesService _stopTimesService;
    private readonly IStopsService _stopsService;
    private readonly ILogger<MetroController> _logger;

    public MetroController(
        IRoutesService routesService,
        ITripsService tripsService,
        IStopTimesService stopTimesService,
        IStopsService stopsService,
        ILogger<MetroController> logger)
    {
        _routesService = routesService;
        _tripsService = tripsService;
        _stopTimesService = stopTimesService;
        _stopsService = stopsService;
        _logger = logger;
    }

    [HttpGet("route-with-trips/{routeId}")]
    public async Task<ActionResult<RouteWithTripsDto>> GetRouteWithTrips(string routeId)
    {
        try
        {
            Models.Route? route = await _routesService.GetByIdAsync(routeId);

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

            List<StopTimeWithStopDto> stopTimesWithStops = new();

            foreach (var stopTime in stopTimes.OrderBy(st => st.StopSequence))
            {
                Stop? stop = await _stopsService.GetByIdAsync(stopTime.StopId);
                if (stop == null) continue;

                stopTimesWithStops.Add(new StopTimeWithStopDto
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
    public async Task<ActionResult<List<StopTimeWithStopDto>>> GetUpcomingDepartures(
        string stopId,
        [FromQuery] int take = 100,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] DateTime? referenceTime = null)
    {
        try
        {
            Stop? stop = await _stopsService.GetByIdAsync(stopId);

            if (stop == null)
                return NotFound(new { message = $"Stop with ID {stopId} not found" });

            DateTime reference = referenceTime ?? DateTime.Now;

            string referenceTimeString = reference.ToString("HH:mm:ss");

            List<StopTime>? stopTimes = await _stopTimesService.GetByStopIdAsync(stopId, page: page, pageSize: pageSize);

            if (stopTimes == null)
                return NotFound(new { message = $"No stop time with {stopId} found" });

            var upcomingDepartures = stopTimes
                .Where(st => string.Compare(st.DepartureTime, referenceTimeString) > 0)
                .OrderBy(st => st.DepartureTime)
                .Take(take)
                .ToList();

            List<StopTimeWithStopDto> result = new();

            Dictionary<string, int> dic = new();

            foreach (var departure in upcomingDepartures)
            {
                Trip? trip = await _tripsService.GetByIdAsync(departure.TripId);
                Models.Route? route = trip != null ? await _routesService.GetByIdAsync(trip.RouteId) : null;

                result.Add(new StopTimeWithStopDto
                {
                    StopTime = departure,
                    Stop = stop
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
}