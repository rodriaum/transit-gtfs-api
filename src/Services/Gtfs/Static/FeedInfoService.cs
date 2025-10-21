using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FeedInfoService : IFeedInfoService
{
    private readonly GtfsDbContext _gtfsDBContext;
    private readonly ILogger<FeedInfoService> _logger;
    private readonly IRedisService _redis;

    public FeedInfoService(GtfsDbContext gtfsDBContext, ILogger<FeedInfoService> logger, IRedisService redis)
    {
        _gtfsDBContext = gtfsDBContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<FeedInfo>> GetAllAsync()
    {
        return await _gtfsDBContext.Set<FeedInfo>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "feed_info.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.SqlBatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingIds = new HashSet<string>(
                await _gtfsDBContext.Set<FeedInfo>().Select(f => f.FeedPublisherName.ToLower() + ":" + f.FeedPublisherUrl.ToLower()).ToListAsync()
            );

            List<FeedInfo> entities = new List<FeedInfo>(batchSize);

            using (StreamReader reader = new StreamReader(filePath))
            {
                string? headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    _logger.LogWarning($"No data found in {filePath}");
                    return;
                }

                string[] headers = headerLine.Split(',');
                string? line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] values = line.Split(',');
                    Dictionary<string, string?> rowData = new Dictionary<string, string?>();

                    for (int j = 0; j < headers.Length; j++)
                    {
                        if (j < values.Length)
                        {
                            rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
                        }
                    }

                    string publisherName = rowData.GetValueOrDefault("feed_publisher_name", "") ?? "";
                    string publisherUrl = rowData.GetValueOrDefault("feed_publisher_url", "") ?? "";
                    string uniqueKey = publisherName.ToLower() + ":" + publisherUrl.ToLower();
                    if (existingIds.Contains(uniqueKey))
                    {
                        totalIgnored++;
                        continue;
                    }

                    FeedInfo entity = new FeedInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        FeedPublisherName = publisherName,
                        FeedPublisherUrl = publisherUrl,
                        FeedLang = rowData.GetValueOrDefault("feed_lang", "") ?? "",
                        FeedStartDate = rowData.GetValueOrDefault("feed_start_date", null),
                        FeedEndDate = rowData.GetValueOrDefault("feed_end_date", null),
                        FeedVersion = rowData.GetValueOrDefault("feed_version", null),
                        FeedContactEmail = rowData.GetValueOrDefault("feed_contact_email", null),
                        FeedContactUrl = rowData.GetValueOrDefault("feed_contact_url", null)
                    };

                    entities.Add(entity);
                    existingIds.Add(uniqueKey);

                    if (entities.Count >= batchSize)
                    {
                        await _gtfsDBContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _gtfsDBContext.BulkInsertAsync(entities);
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Inserted {0} records from {1} in database with {2} line(s) ignored. ({3})",
                totalImported,
                filePath,
                totalIgnored,
                TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            return;
        }
    }
}
