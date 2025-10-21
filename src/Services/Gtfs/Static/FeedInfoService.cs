using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FeedInfoService : IFeedInfoService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<FeedInfoService> _logger;
    private readonly IPostgresService _postgresService;

    public FeedInfoService(GtfsDbContext gtfsDbContext, ILogger<FeedInfoService> logger,
        IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<FeedInfo>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<FeedInfo>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "feed_info.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Set<FeedInfo>()
                .Select(f => f.FeedPublisherName.ToLower() + ":" + f.FeedPublisherUrl.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<FeedInfo> entities = new List<FeedInfo>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
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
            await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
        }
    }
}