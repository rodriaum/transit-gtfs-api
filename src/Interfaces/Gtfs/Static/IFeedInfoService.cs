using TransitGtfsApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IFeedInfoService
{
    Task<List<FeedInfo>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
