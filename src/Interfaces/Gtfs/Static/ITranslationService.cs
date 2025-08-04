using TransitGtfsApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface ITranslationService
{
    Task<List<AgencyTranslation>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
