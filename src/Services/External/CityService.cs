using Microsoft.EntityFrameworkCore;
using Tranzor.Databases;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.External;
using Tranzor.Models.External;

namespace Tranzor.Services.External;

public class CityService : ICityService
{
    private readonly GTFSContext _dbContext;
    private readonly IRedisService _redis;

    public CityService(GTFSContext dbContext, IRedisService redis)
    {
        _dbContext = dbContext;
        _redis = redis;
    }

    public async Task<List<City>> GetAllAsync()
    {
        return await _dbContext.Cities.ToListAsync();
    }

    public async Task<City?> GetByIdAsync(string cityId)
    {
        return await _redis.GetOrSetAsync(
            $"city-{cityId}",
            async () => await _dbContext.Cities.FirstOrDefaultAsync(a => string.Equals(a.Id, cityId, StringComparison.OrdinalIgnoreCase))
        );
    }
}