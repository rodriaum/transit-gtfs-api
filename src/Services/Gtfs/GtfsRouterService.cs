namespace Tranzor.Services.Gtfs;

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Tranzor.Databases;
using Tranzor.Enums;
using Tranzor.Interfaces.Gtfs;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Models.Router;
using Tranzor.Utils;

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
    double fromLat,
    double fromLon,
    double toLat,
    double toLon,
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
                        Mode = RouteLegModeType.None,
                        From = null,
                        To = null,
                        DistanceMeters = directDistance,
                        Duration = TimeSpan.FromSeconds(directDistance / WALKING_SPEED_MPS)
                    }
                }
            }
        };
        }

        List<Stop> originStops = await _stopsService.GetNearestStopAsync(fromLat, fromLon, limit: 1);
        List<Stop> destStops = await _stopsService.GetNearestStopAsync(toLat, toLon, limit: 1);

        Stop? originStop = originStops.FirstOrDefault();
        Stop? destStop = destStops.FirstOrDefault();

        if (originStop == null || destStop == null) return new List<RoutePlan>();

        Dictionary<string, Stop> stopsDict = (await _stopsService.GetAllAsync())
            .ToDictionary(s => s.StopId, s => s);

        // Start A*
        HashSet<string> visited = new HashSet<string>();
        PriorityQueue<AStarNode, double> queue = new PriorityQueue<AStarNode, double>();
        List<RoutePlan> foundRoutes = new List<RoutePlan>();

        queue.Enqueue(new AStarNode
        {
            StopId = originStop.StopId,
            ArrivalTime = departureTime,
            G = 0,
            H = MathUtil.Haversine(originStop.StopLat, originStop.StopLon, destStop.StopLat, destStop.StopLon),
            Path = new List<RouteLeg>()
        }, 0);

        Dictionary<string, List<StopTime>> stopTimesCache = new Dictionary<string, List<StopTime>>();

        while (queue.Count > 0 && foundRoutes.Count < maxRoutes)
        {
            AStarNode current = queue.Dequeue();

            string key = $"{current.StopId}_{current.ArrivalTime.TimeOfDay}";
            if (visited.Contains(key)) continue;

            visited.Add(key);

            if (!stopsDict.TryGetValue(current.StopId, out Stop? stop)) continue;

            double distanceToDest = MathUtil.Haversine(stop.StopLat, stop.StopLon, toLat, toLon);

            if (distanceToDest <= MAX_WALKING_DISTANCE)
            {
                RouteLeg finalLeg = new RouteLeg
                {
                    Mode = RouteLegModeType.Walking,
                    From = stop,
                    To = null,
                    DistanceMeters = distanceToDest,
                    Duration = TimeSpan.FromSeconds(distanceToDest / WALKING_SPEED_MPS)
                };

                List<RouteLeg> fullPath = new List<RouteLeg>(current.Path) { finalLeg };
                TimeSpan totalDuration = current.ArrivalTime - departureTime + finalLeg.Duration.Value;

                foundRoutes.Add(new RoutePlan
                {
                    Duration = totalDuration,
                    Legs = fullPath
                });

                continue;
            }

            List<(string TripId, Route Route)> trips = await GetUpcomingTripsFromStop(current.StopId, current.ArrivalTime);

            foreach ((string tripId, Route route) in trips)
            {
                if (!stopTimesCache.TryGetValue(tripId, out List<StopTime>? stopTimes))
                {
                    stopTimes = await _stopTimesService.GetByTripIdAsync(tripId);
                    stopTimesCache[tripId] = stopTimes;
                }

                int originIndex = stopTimes.FindIndex(s => s.StopId == current.StopId);
                if (originIndex < 0) continue;

                for (int i = originIndex + 1; i < stopTimes.Count; i++)
                {
                    StopTime nextStopTime = stopTimes[i];
                    if (!stopsDict.TryGetValue(nextStopTime.StopId, out Stop? nextStop)) continue;

                    DateTime arrival = departureTime.Date + nextStopTime.ArrivalTimeSpan;

                    double gNew = (arrival - departureTime).TotalSeconds;
                    double hNew = MathUtil.Haversine(nextStop.StopLat, nextStop.StopLon, toLat, toLon);

                    RouteLeg leg = new RouteLeg
                    {
                        Mode = RouteLegModeType.Transit,
                        Route = route,
                        From = stop,
                        To = nextStop,
                        Departure = current.ArrivalTime,
                        Arrival = arrival,
                        Duration = arrival - current.ArrivalTime
                    };

                    List<RouteLeg> newPath = new List<RouteLeg>(current.Path) { leg };

                    AStarNode nextNode = new AStarNode
                    {
                        StopId = nextStop.StopId,
                        ArrivalTime = arrival,
                        G = gNew,
                        H = hNew,
                        Path = newPath
                    };

                    queue.Enqueue(nextNode, gNew + hNew);
                }
            }
        }

        return foundRoutes;
    }

    private async Task<List<(string TripId, Route Route)>> GetUpcomingTripsFromStop(string stopId, DateTime after)
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
                r
            })
            .Distinct()
            .ToList();

        return filtered.Select(x => (x.TripId, x.r)).ToList();
    }
}