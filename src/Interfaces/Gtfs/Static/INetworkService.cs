using TransitGtfsApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface INetworkService
{
    Task<List<Network>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
