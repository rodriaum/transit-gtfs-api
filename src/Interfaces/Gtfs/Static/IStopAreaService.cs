using TransitGtfsApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IStopAreaService
{
    Task<List<StopArea>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
