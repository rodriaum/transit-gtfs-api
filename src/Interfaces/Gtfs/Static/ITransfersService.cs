using Tranzor.Models;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface ITransfersService
{
    Task<List<Transfer>> GetAllAsync();
    Task<List<Transfer>?> GetByFromStopIdAsync(string fromStopId);
    Task ImportDataAsync(string directoryPath);
}