using TransitGtfsApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IAttributionService
{
    Task<List<Attribution>> GetAllAsync();
    Task ImportDataAsync(string directoryPath, string agencyId);
}
