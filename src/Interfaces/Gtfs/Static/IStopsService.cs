using TransitGtfsApi.Models;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IStopsService
{
    Task<List<Stop>> GetAllAsync();
    Task<Stop?> GetByIdAsync(string stopId);
    Task ImportDataAsync(string directoryPath);
    Task<Stop?> GetNearestStopAsync(double lat, double lon);
}