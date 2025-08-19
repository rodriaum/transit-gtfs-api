using Tranzor.Models.OTP;

namespace Tranzor.Interfaces.Gtfs;

public interface IOpenTripPlannerService
{
    Task<List<RoutePlan>> PlanRouteAsync(double fromLat, double fromLon, double toLat, double toLon, DateTime departureTime, int maxRoutes);
}