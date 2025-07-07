using TransitGtfsApi.Models;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IStopTimesService
{
    Task<List<StopTime>> GetAllAsync(int page = 1, int pageSize = 100);
    Task<List<StopTime>?> GetByTripIdAsync(string tripId);
    Task<List<StopTime>?> GetByStopIdAsync(string stopId, int page = 1, int pageSize = 100);
    Task ImportDataAsync(string directoryPath);
}