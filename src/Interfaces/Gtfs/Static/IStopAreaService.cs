using Tranzor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IStopAreaService
{
    Task<List<StopArea>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
