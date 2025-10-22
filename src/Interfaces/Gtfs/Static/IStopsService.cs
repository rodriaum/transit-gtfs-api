using Tranzor.Models;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IStopsService
{
    Task<List<Stop>> GetAllAsync(string? cityId = null, int page = 1, int pageSize = 100);
    Task<Stop?> GetByIdAsync(string stopId);
    Task<List<Stop>> GetStopsBatchAsync(List<string> stopIds);
    Task ImportDataAsync(string directoryPath);
    Task<List<Stop>> GetNearestStopAsync(double lat, double lon, string? cityId = null, int limit = 1);
}