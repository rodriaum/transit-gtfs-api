using Tranzor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tranzor.Interfaces.Gtfs.Static;

public interface ITranslationService
{
    Task<List<AgencyTranslation>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
