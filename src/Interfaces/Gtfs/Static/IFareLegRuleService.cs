using Tranzor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IFareLegRuleService
{
    Task<List<FareLegRule>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
