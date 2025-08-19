using Tranzor.Models.Gtfs.External;

namespace Tranzor.Interfaces.Gtfs.External;

public interface ICityService
{
    Task<List<City>> GetAllAsync();
    Task<City?> GetByIdAsync(string cityId);
}
