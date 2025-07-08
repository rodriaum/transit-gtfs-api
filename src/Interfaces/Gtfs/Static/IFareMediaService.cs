using TransitGtfsApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IFareMediaService
{
    Task<List<FareMedia>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
