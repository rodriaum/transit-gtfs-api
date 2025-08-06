using Tranzor.Models;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface IFareAttributesService
{
    Task<List<FareAttribute>> GetAllAsync();
    Task<FareAttribute?> GetByIdAsync(string fareId);
    Task ImportDataAsync(string directoryPath);
}