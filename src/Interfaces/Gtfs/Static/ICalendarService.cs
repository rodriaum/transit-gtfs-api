using TransitGtfsApi.Models;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface ICalendarService
{
    Task<List<Calendar>> GetAllAsync();
    Task<Calendar?> GetByIdAsync(string serviceId);
    Task ImportDataAsync(string directoryPath);
}