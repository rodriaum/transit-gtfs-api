using TransitGtfsApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IFareProductService
{
    Task<List<FareProduct>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
