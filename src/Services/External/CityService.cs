using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Gtfs.External;
using Tranzor.Models.External;

namespace Tranzor.Services.External;

public class CityService : ICityService
{
    private readonly GtfsDbContext _gtfsDbContext;

    public CityService(GtfsDbContext gtfsDbContext)
    {
        _gtfsDbContext = gtfsDbContext;
    }

    public async Task<List<City>> GetAllAsync()
    {
        return await _gtfsDbContext.Cities.ToListAsync();
    }

    public async Task<City?> GetByIdAsync(string cityId)
    {
        return await _gtfsDbContext.Cities.FirstOrDefaultAsync(a => string.Equals(a.Id, cityId, StringComparison.OrdinalIgnoreCase));
    }
}