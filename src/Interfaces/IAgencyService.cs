using TransitGtfsApi.Models;

namespace TransitGtfsApi.Interfaces;

public interface IAgencyService
{
    Task<List<Agency>> GetAllAsync();
    Task<Agency?> GetByIdAsync(string agencyId);
    Task ImportDataAsync(string directoryPath, string? agencyKey = null);
}