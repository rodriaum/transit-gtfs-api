using Tranzor.Models;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IFareRulesService
{
    Task<List<FareRule>> GetAllAsync();
    Task<List<FareRule>?> GetByFareIdAsync(string fareId);
    Task ImportDataAsync(string directoryPath);
}