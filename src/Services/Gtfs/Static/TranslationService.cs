using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class TranslationService : ITranslationService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<TranslationService> _logger;
    private readonly IPostgresService _postgresService;

    public TranslationService(GtfsDbContext gtfsDbContext, ILogger<TranslationService> logger,
        IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<AgencyTranslation>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<AgencyTranslation>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "translations.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Set<AgencyTranslation>().Select(t =>
                t.TableName.ToLower() + ":" + t.FieldName.ToLower() + ":" + t.Language.ToLower() + ":" +
                (t.RecordId ?? "")).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<AgencyTranslation> entities = new List<AgencyTranslation>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string tableName = rowData.GetValueOrDefault("table_name", "") ?? "";
            string fieldName = rowData.GetValueOrDefault("field_name", "") ?? "";
            string language = rowData.GetValueOrDefault("language", "") ?? "";
            string recordId = rowData.GetValueOrDefault("record_id", "") ?? "";
            string uniqueKey = tableName.ToLower() + ":" + fieldName.ToLower() + ":" + language.ToLower() + ":" +
                               recordId.ToLower();

            if (existingIds.Contains(uniqueKey))
            {
                totalIgnored++;
                continue;
            }

            AgencyTranslation entity = new AgencyTranslation
            {
                Id = Guid.NewGuid().ToString(),
                TableName = tableName,
                FieldName = fieldName,
                Language = language,
                TranslationText = rowData.GetValueOrDefault("translation", "") ?? "",
                RecordId = recordId,
                RecordSubId = rowData.GetValueOrDefault("record_sub_id", null)
            };

            entities.Add(entity);
            existingIds.Add(uniqueKey);
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}