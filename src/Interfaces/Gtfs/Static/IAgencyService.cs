using TransitGtfsApi.Models;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IAgencyService
{
    Task<List<Agency>> GetAllAsync();
    Task<Agency?> GetByIdAsync(string agencyId);
    Task<bool> ImportDataAsync(string directoryPath, string agencyId);
}