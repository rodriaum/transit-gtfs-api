using System.Diagnostics;
using Tranzor.Context;
using Tranzor.Enums;
using Tranzor.Interfaces.Gtfs;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Models.Config;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs;

public class GtfsDataService : IGtfsDataService
{
    private readonly IGtfsFileService _gtfsFileService;
    private readonly IAgencyService _agencyService;
    private readonly ICalendarService _calendarService;
    private readonly ICalendarDatesService _calendarDatesService;
    private readonly IFareAttributesService _fareAttributesService;
    private readonly IFareRulesService _fareRulesService;
    private readonly IRoutesService _routesService;
    private readonly IShapesService _shapesService;
    private readonly IStopsService _stopsService;
    private readonly IStopTimesService _stopTimesService;
    private readonly ITransfersService _transfersService;
    private readonly ITripsService _tripsService;
    private readonly IFeedInfoService _feedInfoService;
    private readonly ITranslationService _translationService;
    private readonly IAttributionService _attributionService;
    private readonly IStopAreaService _stopAreaService;
    private readonly IFareMediaService _fareMediaService;
    private readonly IFareLegRuleService _fareLegRuleService;
    private readonly IFareProductService _fareProductService;
    private readonly INetworkService _networkService;
    private readonly ILogger<GtfsDataService> _logger;

    public GtfsDataService(
        IGtfsFileService gtfsFileService,
        IAgencyService agencyService,
        ICalendarService calendarService,
        ICalendarDatesService calendarDatesService,
        IFareAttributesService fareAttributesService,
        IFareRulesService fareRulesService,
        IRoutesService routesService,
        IShapesService shapesService,
        IStopsService stopsService,
        IStopTimesService stopTimesService,
        ITransfersService transfersService,
        ITripsService tripsService,
        IFeedInfoService feedInfoService,
        ITranslationService translationService,
        IAttributionService attributionService,
        IStopAreaService stopAreaService,
        IFareMediaService fareMediaService,
        IFareLegRuleService fareLegRuleService,
        IFareProductService fareProductService,
        INetworkService networkService,
        ILogger<GtfsDataService> logger)
    {
        _gtfsFileService = gtfsFileService;
        _agencyService = agencyService;
        _calendarService = calendarService;
        _calendarDatesService = calendarDatesService;
        _fareAttributesService = fareAttributesService;
        _fareRulesService = fareRulesService;
        _routesService = routesService;
        _shapesService = shapesService;
        _stopsService = stopsService;
        _stopTimesService = stopTimesService;
        _transfersService = transfersService;
        _tripsService = tripsService;
        _feedInfoService = feedInfoService;
        _translationService = translationService;
        _attributionService = attributionService;
        _stopAreaService = stopAreaService;
        _fareMediaService = fareMediaService;
        _fareLegRuleService = fareLegRuleService;
        _fareProductService = fareProductService;
        _networkService = networkService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await LoadDataFromFilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing GTFS data service");
            throw;
        }
    }

    public async Task LoadDataFromFilesAsync()
    {
        if (GtfsDataContext.Config.DownloadData == DownloadDataType.None)
        {
            _logger.LogInformation("Import option is None, nothing will be imported.");
            return;
        }
        else if (GtfsDataContext.Config.DownloadData == DownloadDataType.FirstUse)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string flagFile = Path.Combine(baseDirectory, "api_started.flag");

            try
            {
                if (!File.Exists(flagFile))
                {
                    File.WriteAllText(flagFile, DateTime.Now.ToString("O"));
                    _logger.LogInformation("First use detected. Flag file created at {FlagFile}", flagFile);
                }
                else
                {
                    _logger.LogInformation("Flag file already exists ({FlagFile}), skipping import.", flagFile);
                    return;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "No permission to write flag file in {BaseDirectory}", baseDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while writing flag file {FlagFile}", flagFile);
            }
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            List<string> gtfsDirectories = await _gtfsFileService.EnsureGtfsFilesExistAsync();

            if (gtfsDirectories.Count == 0)
            {
                _logger.LogWarning("No GTFS directory found for import.");
                return;
            }

            _logger.LogInformation("Starting import of data from {0} GTFS sources", gtfsDirectories.Count);

            List<Agency> existingAgencies = await _agencyService.GetAllAsync();

            for (int i = 0; i < gtfsDirectories.Count; i++)
            {
                string gtfsDirectoryPath = gtfsDirectories[i];
                string agencyIdByPath = gtfsDirectoryPath.Split(Path.DirectorySeparatorChar.ToString()).Last();

                GtfsData? gtfsData = GtfsDataContext.GtfsDataList.Find(it => string.Equals(it.AgencyId, agencyIdByPath, StringComparison.OrdinalIgnoreCase));

                if (gtfsData == null)
                {
                    _logger.LogWarning("No GTFS data configuration found for directory {0}. Skipping import.", gtfsDirectoryPath);
                    continue;
                }

                string? agencyId = gtfsData.AgencyId;

                if (string.IsNullOrWhiteSpace(agencyId))
                {
                    _logger.LogInformation("Agency key at index {0} cannot be null.", i);
                    continue;
                }

                if (existingAgencies.Find(it => !string.IsNullOrWhiteSpace(it.AgencyId) && string.Equals(it.AgencyId, agencyId, StringComparison.OrdinalIgnoreCase)) != null)
                {
                    _logger.LogInformation("Ignoring {0} becauses already exists.", gtfsData.AgencyId);
                    continue;
                }

                _logger.LogInformation("Importing GTFS data from agency {0}", agencyId);

                /*
                * Because agencies are very creative with their agency_id, such as SUPER Creative values like "1", "2"
                * Where several use this, and end up duplicating or replacing, the id will be set manually based on the GtfsData
                * from the gtfs data list which contains the download url and other information.
                */

                // This is always obligatory, as it contains agency information
                if (!await _agencyService.ImportDataAsync(gtfsDirectoryPath, agencyId))
                {
                    _logger.LogWarning($"Agency {agencyId} could not be imported.");
                    continue;
                }

                await ImportFileIfExists(gtfsDirectoryPath, "calendar.txt", gtfsData.IgnoredFiles,
                    async () => await _calendarService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "calendar_dates.txt", gtfsData.IgnoredFiles,
                    async () => await _calendarDatesService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "fare_attributes.txt", gtfsData.IgnoredFiles,
                    async () => await _fareAttributesService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "fare_rules.txt", gtfsData.IgnoredFiles,
                    async () => await _fareRulesService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "routes.txt", gtfsData.IgnoredFiles,
                    async () => await _routesService.ImportDataAsync(gtfsDirectoryPath, agencyId));

                await ImportFileIfExists(gtfsDirectoryPath, "shapes.txt", gtfsData.IgnoredFiles,
                    async () => await _shapesService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "stops.txt", gtfsData.IgnoredFiles,
                    async () => await _stopsService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "stop_times.txt", gtfsData.IgnoredFiles,
                    async () => await _stopTimesService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "transfers.txt", gtfsData.IgnoredFiles,
                    async () => await _transfersService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "trips.txt", gtfsData.IgnoredFiles,
                    async () => await _tripsService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "feed_info.txt", gtfsData.IgnoredFiles,
                    async () => await _feedInfoService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "translations.txt", gtfsData.IgnoredFiles,
                    async () => await _translationService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "attributions.txt", gtfsData.IgnoredFiles,
                    async () => await _attributionService.ImportDataAsync(gtfsDirectoryPath, agencyId));

                await ImportFileIfExists(gtfsDirectoryPath, "stop_areas.txt", gtfsData.IgnoredFiles,
                    async () => await _stopAreaService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "fare_media.txt", gtfsData.IgnoredFiles,
                    async () => await _fareMediaService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "fare_leg_rules.txt", gtfsData.IgnoredFiles,
                    async () => await _fareLegRuleService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "fare_products.txt", gtfsData.IgnoredFiles,
                    async () => await _fareProductService.ImportDataAsync(gtfsDirectoryPath));

                await ImportFileIfExists(gtfsDirectoryPath, "networks.txt", gtfsData.IgnoredFiles,
                    async () => await _networkService.ImportDataAsync(gtfsDirectoryPath));

                _logger.LogInformation("Data import from {0} completed", gtfsDirectoryPath);
            }

            _logger.LogInformation("Import of all GTFS data completed");
        }
        catch (Exception ex)
        {
            throw new Exception("Error loading data from GTFS files", ex);
        }
        finally
        {
            stopwatch.Stop();

            string duration = TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation("Total GTFS import duration: {0}", duration);
        }
    }

    private async Task ImportFileIfExists(string directoryPath, string fileName, List<string> ignoredFiles, Func<Task> importAction)
    {
        if (!fileName.EndsWith(".txt"))
            fileName += ".txt";

        if (ignoredFiles.Exists(it => it.StartsWith(fileName)))
        {
            _logger.LogDebug($"Skipping import of ignored file: {fileName}");
            return;
        }

        string filePath = Path.Combine(directoryPath, fileName);

        if (File.Exists(filePath))
        {
            _logger.LogDebug("Importing {0}", fileName);
            await importAction();
        }
        else
        {
            _logger.LogWarning("File {0} not found in {1}", fileName, directoryPath);
        }
    }
}