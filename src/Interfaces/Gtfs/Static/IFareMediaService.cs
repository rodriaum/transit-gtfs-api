using Tranzor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IFareMediaService
{
    Task<List<FareMedia>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
