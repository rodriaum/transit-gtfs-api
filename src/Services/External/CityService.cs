using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.External;
using Tranzor.Models.External;

namespace Tranzor.Services.External;

public class CityService : ICityService
{
    private readonly GtfsDbContext _gtfsDBContext;
    private readonly IRedisService _redis;

    public CityService(GtfsDbContext gtfsDBContext, IRedisService redis)
    {
        _gtfsDBContext = gtfsDBContext;
        _redis = redis;
    }

    public async Task<List<City>> GetAllAsync()
    {
        return await _gtfsDBContext.Cities.ToListAsync();
    }

    public async Task<City?> GetByIdAsync(string cityId)
    {
        return await _redis.GetOrSetAsync(
            $"city-{cityId}",
            async () => await _gtfsDBContext.Cities.FirstOrDefaultAsync(a => string.Equals(a.Id, cityId, StringComparison.OrdinalIgnoreCase))
        );
    }
}