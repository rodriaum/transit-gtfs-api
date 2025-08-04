using TransitGtfsApi.Models.Router;

namespace TransitGtfsApi.Interfaces.Gtfs;

public interface IGtfsRouterService
{
    Task<List<RoutePlan>> PlanRouteAsync(
        double fromLat, double fromLon,
        double toLat, double toLon,
        DateTime departureTime,
        int maxRoutes = 1);
}