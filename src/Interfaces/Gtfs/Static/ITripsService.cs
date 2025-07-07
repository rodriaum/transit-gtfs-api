using TransitGtfsApi.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface ITripsService
{
    Task<List<Trip>> GetAllAsync(int page = 1, int pageSize = 100);
    Task<Trip?> GetByIdAsync(string tripId);
    Task<List<Trip>?> GetByRouteIdAsync(string routeId, int page = 1, int pageSize = 100);
    Task<List<Trip>?> GetTripsBatchAsync(List<string> tripIds);
    Task ImportDataAsync(string directoryPath);
}