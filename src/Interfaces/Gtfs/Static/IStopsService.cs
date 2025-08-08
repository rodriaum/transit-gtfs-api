using Tranzor.Models;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IStopsService
{
    Task<List<Stop>> GetAllAsync(string? cityId = null);
    Task<Stop?> GetByIdAsync(string stopId);
    Task ImportDataAsync(string directoryPath);
    Task<List<Stop>> GetNearestStopAsync(double lat, double lon, string? cityId = null, int limit = 1);
}