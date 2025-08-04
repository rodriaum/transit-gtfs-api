namespace TransitGtfsApi.Services.Gtfs;

using Microsoft.EntityFrameworkCore;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Interfaces.Gtfs;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;
using TransitGtfsApi.Models.Router;
using TransitGtfsApi.Utils;

public class GtfsRouterService : IGtfsRouterService
{
    private readonly IStopsService _stopsService;
    private readonly IStopTimesService _stopTimesService;
    private readonly GTFSContext _context;
    private readonly double WALKING_SPEED_MPS = 1.3; // m/s (~4.7 km/h)
    private readonly double MAX_WALKING_DISTANCE = 800;
    private readonly double MIN_DISTANCE_THRESHOLD = 100;

    public GtfsRouterService(IStopsService stopsService, IStopTimesService stopTimesService, GTFSContext context)
    {
        _stopsService = stopsService;
        _stopTimesService = stopTimesService;
        _context = context;
    }

    public async Task<List<RoutePlan>> PlanRouteAsync(
        double fromLat, double fromLon,
        double toLat, double toLon,
        DateTime departureTime,
        int maxRoutes = 1)
    {
        double directDistance = MathUtil.Haversine(fromLat, fromLon, toLat, toLon);

        if (directDistance < MIN_DISTANCE_THRESHOLD)
        {
            return new List<RoutePlan>
        {
            new RoutePlan
            {
                Duration = TimeSpan.FromSeconds(directDistance / WALKING_SPEED_MPS),
                Legs = new List<RouteLeg>
                {
                    new RouteLeg
                    {
                        Mode = Constant.ModeWalkingKey,
                        From = "Origem",
                        To = "Destino",
                        DistanceMeters = directDistance,
                        Duration = TimeSpan.FromSeconds(directDistance / WALKING_SPEED_MPS)
                    }
                }
            }
        };
        }

        Stop? originStop = await _stopsService.GetNearestStopAsync(fromLat, fromLon);
        Stop? destStop = await _stopsService.GetNearestStopAsync(toLat, toLon);

        if (originStop == null || destStop == null)
            return new List<RoutePlan>();

        HashSet<string> visited = new HashSet<string>();
        PriorityQueue<AStarNode, double> queue = new PriorityQueue<AStarNode, double>();
        List<RoutePlan> resultRoutes = new List<RoutePlan>();

        queue.Enqueue(new AStarNode
        {
            StopId = originStop.StopId,
            ArrivalTime = departureTime,
            G = 0,
            H = MathUtil.Haversine(originStop.StopLat, originStop.StopLon, destStop.StopLat, destStop.StopLon),
            Path = new List<RouteLeg>()
        }, 0);

        while (queue.Count > 0 && resultRoutes.Count < maxRoutes)
        {
            AStarNode current = queue.Dequeue();
            string key = $"{current.StopId}_{current.ArrivalTime.TimeOfDay}";

            if (visited.Contains(key)) continue;

            visited.Add(key);

            Stop? stop = await _stopsService.GetByIdAsync(current.StopId);
            if (stop == null) continue;

            double distanceToDestination = MathUtil.Haversine(stop.StopLat, stop.StopLon, toLat, toLon);

            if (distanceToDestination <= MAX_WALKING_DISTANCE)
            {
                RouteLeg finalLeg = new RouteLeg
                {
                    Mode = "walking",
                    From = stop.StopName,
                    To = "Destino",
                    DistanceMeters = distanceToDestination,
                    Duration = TimeSpan.FromSeconds(distanceToDestination / WALKING_SPEED_MPS)
                };

                current.Path.Add(finalLeg);

                resultRoutes.Add(new RoutePlan
                {
                    Duration = current.ArrivalTime - departureTime + finalLeg.Duration.Value,
                    Legs = current.Path
                });

                continue;
            }

            List<(string TripId, string RouteShortName)> trips = await GetUpcomingTripsFromStop(current.StopId, current.ArrivalTime);

            foreach ((string TripId, string RouteShortName) trip in trips)
            {
                List<StopTime> stopTimes = await _stopTimesService.GetStopTimesForTrip(trip.TripId);
                int originIndex = stopTimes.FindIndex(s => s.StopId == current.StopId);

                for (int i = originIndex + 1; i < stopTimes.Count; i++)
                {
                    StopTime nextStop = stopTimes[i];
                    Stop? nextStopEntity = await _stopsService.GetByIdAsync(nextStop.StopId);

                    if (nextStopEntity == null) continue;

                    double gNew = (nextStop.ArrivalTimeSpan - departureTime.TimeOfDay).TotalSeconds;
                    double hNew = MathUtil.Haversine(nextStopEntity.StopLat, nextStopEntity.StopLon, toLat, toLon);

                    List<RouteLeg> newPath = new List<RouteLeg>(current.Path)
                {
                    new RouteLeg
                    {
                        Mode = "transit",
                        Route = trip.RouteShortName,
                        From = stop.StopName,
                        To = nextStopEntity.StopName,
                        Departure = current.ArrivalTime,
                        Arrival = departureTime.Date + nextStop.ArrivalTimeSpan,
                    }
                };

                    queue.Enqueue(new AStarNode
                    {
                        StopId = nextStop.StopId,
                        ArrivalTime = departureTime.Date + nextStop.ArrivalTimeSpan,
                        G = gNew,
                        H = hNew,
                        Path = newPath
                    }, gNew + hNew);
                }
            }
        }

        return resultRoutes;
    }

    private async Task<List<(string TripId, string RouteShortName)>> GetUpcomingTripsFromStop(string stopId, DateTime after)
    {
        var time = after.TimeOfDay;

        var stopTimes = await _context.StopTimes
            .Where(s => s.StopId == stopId)
            .ToListAsync();

        var filtered = stopTimes
            .Where(s => s.DepartureTimeSpan >= time)
            .Join(_context.Trips, s => s.TripId, t => t.TripId, (s, t) => new { s, t })
            .Join(_context.Routes, st => st.t.RouteId, r => r.RouteId, (st, r) => new
            {
                st.t.TripId,
                r.RouteShortName
            })
            .Distinct()
            .ToList();

        return filtered.Select(x => (x.TripId, x.RouteShortName)).ToList();
    }
}