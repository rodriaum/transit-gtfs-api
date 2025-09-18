using Tranzor.Models.External;

namespace Tranzor.Interfaces.Gtfs.External;

public interface ICityService
{
    Task<List<City>> GetAllAsync();
    Task<City?> GetByIdAsync(string cityId);
}
