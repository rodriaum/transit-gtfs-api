using System.Diagnostics;
using TransitGtfsApi.Utils;
using TransitGtfsApi.Interfaces.Gtfs;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Services.Gtfs;

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
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            List<string> gtfsDirectories = await _gtfsFileService.EnsureGtfsFilesExistAsync();

            if (gtfsDirectories.Count == 0)
            {
                _logger.LogWarning("No GTFS directory found for import.");
                return;
            }

            _logger.LogInformation($"Starting import of data from {gtfsDirectories.Count} GTFS sources");

            List<Agency> existingAgencies = await _agencyService.GetAllAsync();

            for (int i = 0; i < gtfsDirectories.Count; i++)
            {
                string gtfsDirectoryPath = gtfsDirectories[i];
                string agencyKeyByPath = gtfsDirectoryPath.Split(Path.DirectorySeparatorChar.ToString()).Last();

                GtfsData? gtfsData = Constant.GtfsDataList.Find(it => it.AgencyId == agencyKeyByPath);

                if (gtfsData == null)
                {
                    _logger.LogWarning($"No GTFS data configuration found for directory {gtfsDirectoryPath}. Skipping import.");
                    continue;
                }

                string? agencyKey = gtfsData.AgencyId;

                if (string.IsNullOrWhiteSpace(agencyKey))
                {
                    _logger.LogInformation($"Agency key at index {i} cannot be null.");
                    continue;
                }

                if (existingAgencies.Find(it => !string.IsNullOrWhiteSpace(it.AgencyId) && it.AgencyId == agencyKey) != null)
                {
                    _logger.LogInformation($"Ignoring {gtfsData.AgencyId} becauses already exists.");
                    continue;
                }

                _logger.LogInformation($"Importing GTFS data from {gtfsDirectoryPath} (Agency: {agencyKey})");

                /**
                 * Using agencyKey in agency and routes services is because
                 * some agencies forget or do not put the agencyId, and this
                 * ends up causing internal problems when searching for id.
                 */

                // This is always obligatory, as it contains agency information
                if (!await _agencyService.ImportDataAsync(gtfsDirectoryPath, agencyKey))
                {
                    _logger.LogWarning($"Agency {agencyKey} could not be imported.");
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
                    async () => await _routesService.ImportDataAsync(gtfsDirectoryPath, agencyKey));

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

                _logger.LogInformation($"Data import from {gtfsDirectoryPath} completed successfully");
            }

            _logger.LogInformation("Import of all GTFS data completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data from GTFS files");
        }
        finally
        {
            stopwatch.Stop();

            string duration = TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"Total GTFS import duration: {duration}");
        }
    }

    private async Task ImportFileIfExists(string directoryPath, string fileName, List<string> ignoredFiles, Func<Task> importAction)
    {
        if (ignoredFiles.Contains(fileName))
        {
            _logger.LogDebug($"Skipping import of ignored file: {fileName}");
            return;
        }

        string filePath = Path.Combine(directoryPath, fileName);
        if (File.Exists(filePath))
        {
            _logger.LogDebug($"Importing {fileName}");
            await importAction();
        }
        else
        {
            _logger.LogWarning($"File {fileName} not found in {directoryPath}");
        }
    }
}