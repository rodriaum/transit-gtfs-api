using TransitGtfsApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IFeedInfoService
{
    Task<List<FeedInfo>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}

public interface ITranslationService
{
    Task<List<Translation>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}

public interface IAttributionService
{
    Task<List<Attribution>> GetAllAsync();
    Task ImportDataAsync(string directoryPath, string agencyId);
}

public interface IStopAreaService
{
    Task<List<StopArea>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}

public interface IFareMediaService
{
    Task<List<FareMedia>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}

public interface IFareLegRuleService
{
    Task<List<FareLegRule>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}

public interface IFareProductService
{
    Task<List<FareProduct>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}

public interface INetworkService
{
    Task<List<Network>> GetAllAsync();
    Task ImportDataAsync(string directoryPath);
}
