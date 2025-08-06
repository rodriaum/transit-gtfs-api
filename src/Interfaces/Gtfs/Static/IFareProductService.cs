using Tranzor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IFareProductService
{
    Task<List<FareProduct>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
