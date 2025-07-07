using System.IO.Compression;
using TransitGtfsApi.Interfaces.Gtfs;
using TransitGtfsApi.Models;

namespace TransitGtfsApi.Services.Gtfs;

public class GtfsFileService : IGtfsFileService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GtfsFileService> _logger;

    public GtfsFileService(IHttpClientFactory httpClientFactory, ILogger<GtfsFileService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // TODO: Verify why is returning only one item
    public async Task<List<string>> EnsureGtfsFilesExistAsync()
    {
        List<string> gtfsDirectories = new List<string>();

        if (!Directory.Exists(Constant.ExtractPath))
        {
            Directory.CreateDirectory(Constant.ExtractPath);
        }

        if (Constant.GtfsDataList.Count == 0)
        {
            _logger.LogWarning("No GTFS URLs configured. Please configure at least one URL in Constant.GtfsFileUrls.");
            return gtfsDirectories;
        }

        for (int i = 0; i < Constant.GtfsDataList.Count; i++)
        {
            GtfsData gtfsData = Constant.GtfsDataList[i];

            string gtfsUrl = gtfsData.Url;
            string providerFolderName = gtfsData.AgencyId;
            string providerDirectory = Path.Combine(Constant.ExtractPath, providerFolderName);

            if (!Directory.Exists(providerDirectory))
            {
                Directory.CreateDirectory(providerDirectory);
            }

            List<string> ignoredFiles = gtfsData.IgnoredFiles;

            bool needsDownload = !AreRequiredFilesPresent(providerDirectory, ignoredFiles);

            if (needsDownload)
            {
                _logger.LogInformation($"Downloading and extracting GTFS data from {gtfsUrl} to {providerDirectory}");
                string tempZipPath = Path.Combine(Constant.TempDownloadFolder, $"gtfs_{i + 1}.zip");

                if (!Directory.Exists(Constant.TempDownloadFolder))
                {
                    Directory.CreateDirectory(Constant.TempDownloadFolder);
                }

                if (!await DownloadGtfsFileAsync(gtfsUrl, tempZipPath))
                    continue;

                ExtractGtfsFile(tempZipPath, providerDirectory, ignoredFiles);

                try
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not delete temporary file {tempZipPath}");
                }
            }
            else
            {
                _logger.LogInformation($"GTFS files for {providerFolderName} already exist at {providerDirectory}");
            }

            gtfsDirectories.Add(providerDirectory);
        }

        try
        {
            if (Directory.Exists(Constant.TempDownloadFolder))
            {
                Directory.Delete(Constant.TempDownloadFolder, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete temporary directory. Some temporary files may remain.");
        }

        return gtfsDirectories;
    }

    private bool AreRequiredFilesPresent(string directoryPath, List<string> ignoredFiles)
    {
        string[] requiredFiles = { "agency.txt", "calendar.txt", "calendar_dates.txt", "fare_attributes.txt",
                               "fare_rules.txt", "routes.txt", "shapes.txt", "stops.txt",
                               "stop_times.txt", "transfers.txt", "trips.txt" };

        return requiredFiles
            .Where(file => !ignoredFiles.Contains(file))
            .All(file => File.Exists(Path.Combine(directoryPath, file)));
    }

    private async Task<bool> DownloadGtfsFileAsync(string url, string filePath)
    {
        _logger.LogInformation($"Downloading GTFS data from {url}");

        try
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            _logger.LogInformation($"Download completed successfully for {url}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading GTFS data from {url}");
            return false;
        }
    }

    private void ExtractGtfsFile(string zipFilePath, string extractPath, List<string> ignoredFiles)
    {
        _logger.LogInformation($"Extracting GTFS data from {zipFilePath} to {extractPath}");

        if (Directory.Exists(extractPath))
        {
            DirectoryInfo di = new DirectoryInfo(extractPath);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }

        try
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (ignoredFiles.Contains(entry.Name))
                    {
                        _logger.LogDebug($"Skipping ignored file: {entry.Name}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    string destinationPath = Path.Combine(extractPath, entry.Name);

                    try
                    {
                        entry.ExtractToFile(destinationPath, overwrite: true);
                        _logger.LogDebug($"Extracted file: {entry.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to extract file: {entry.Name}. Skipping.");
                    }
                }
            }

            _logger.LogInformation("Extraction completed (with possible skipped files).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error extracting GTFS data from {zipFilePath}");
        }
    }
}
