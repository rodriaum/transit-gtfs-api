using Tranzor.Models;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IStopTimesService
{
    Task<List<StopTime>> GetAllAsync(int page = 1, int pageSize = 100);
    Task<List<StopTime>?> GetByTripIdAsync(string tripId, bool ignoreCalendar = false);
    Task<List<StopTime>?> GetByStopIdAsync(string stopId, int? page = null, int? pageSize = null, bool ignoreCalendar = false);
    Task<List<StopTime>?> GetUpcomingDeparturesByStopIdAsync(string stopId, int page = 1, int pageSize = 3, bool ignoreCalendar = false, DateTime? referenceTime = null);
    Task ImportDataAsync(string directoryPath);
}