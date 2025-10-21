using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
    private readonly IRedisService _redis;

    public TranslationService(GtfsDbContext gtfsDbContext, ILogger<TranslationService> logger, IRedisService redis)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<AgencyTranslation>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<AgencyTranslation>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "translations.txt");

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
                await _gtfsDbContext.Set<AgencyTranslation>().Select(t => t.TableName.ToLower() + ":" + t.FieldName.ToLower() + ":" + t.Language.ToLower() + ":" + (t.RecordId ?? "")).ToListAsync()
            );

            List<AgencyTranslation> entities = new List<AgencyTranslation>(batchSize);

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

                    string tableName = rowData.GetValueOrDefault("table_name", "") ?? "";
                    string fieldName = rowData.GetValueOrDefault("field_name", "") ?? "";
                    string language = rowData.GetValueOrDefault("language", "") ?? "";
                    string recordId = rowData.GetValueOrDefault("record_id", "") ?? "";
                    string uniqueKey = tableName.ToLower() + ":" + fieldName.ToLower() + ":" + language.ToLower() + ":" + recordId.ToLower();
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

                    if (entities.Count >= batchSize)
                    {
                        await _gtfsDbContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _gtfsDbContext.BulkInsertAsync(entities);
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
