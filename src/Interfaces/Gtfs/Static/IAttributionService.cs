using Tranzor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IAttributionService
{
    Task<List<Attribution>> GetAllAsync();
    Task ImportDataAsync(string directoryPath, string agencyId);
}
