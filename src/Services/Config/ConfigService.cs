using Tranzor.Context;
using Tranzor.Interfaces.Config;
using Tranzor.Models.Config;
using Tranzor.Utils;

namespace Tranzor.Services.Config;

public class ConfigService : IConfigService
{
    private readonly ILogger<ConfigService> _logger;

    public ConfigService(ILogger<ConfigService> logger)
    {
        this._logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            (bool isConfigFileIntegrity, string configFilePath) = VerifyConfigFileIntegrity("config.json");
            (bool isGtfsDataFileIntegrity, string gtfsDataFilePath) = VerifyConfigFileIntegrity("gtfs_data.json");

            ConfigData config = new ConfigData(false);

            if (isConfigFileIntegrity)
            {
                ConfigData? loadedConfig = await JsonUtil.JsonFileToObjectAsync<ConfigData>(configFilePath);
                if (loadedConfig != null)
                {
                    config = loadedConfig;

                    if (isGtfsDataFileIntegrity && config.DownloadData)
                    {
                        List<GtfsData>? gtfsDataList = await JsonUtil.JsonFileToObjectAsync<List<GtfsData>>(gtfsDataFilePath);
                        if (gtfsDataList != null)
                        {
                            GtfsDataContext.GtfsDataList = gtfsDataList;
                        }
                    }
                }
            }

            GtfsDataContext.Config = config;
            GtfsDataContext.Finish = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing config service");
            throw;
        }
    }

    private (bool, string) VerifyConfigFileIntegrity(string file)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string? path = Path.Combine(baseDirectory, "Data", file);

        if (File.Exists(path))
            return (true, path);

        path = Path.Combine(baseDirectory, "..", "..", "..", "..", "Data", file);

        if (File.Exists(path))
            return (true, path);

        _logger.LogWarning("Could not find {File} file, import will be turned off even if it is enabled in the configuration file.", file);
        return (false, file);
    }
}