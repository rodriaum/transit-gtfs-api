using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Tranzor.Databases;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class CalendarDatesService : ICalendarDatesService
{
    private readonly GTFSContext _dbContext;
    private readonly ILogger<CalendarDatesService> _logger;
    private readonly IRedisService _redis;

    public CalendarDatesService(GTFSContext dbContext, ILogger<CalendarDatesService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<CalendarDate>> GetAllAsync()
    {
        return await _dbContext.CalendarDates.ToListAsync();
    }

    public async Task<List<CalendarDate>?> GetByServiceIdAsync(string serviceId)
    {
        return await _redis.GetOrSetAsync(
            $"calendar-dates-service-{serviceId}",
            async () => await _dbContext.CalendarDates.Where(c => c.ServiceId == serviceId).ToListAsync()
        ) ?? new List<CalendarDate>();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "calendar_dates.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return;
        }

        try
        {
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.BatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingIds = new HashSet<string>(
                await _dbContext.CalendarDates.Select(c => c.ServiceId.ToLower() + ":" + c.Date.ToLower()).ToListAsync()
            );

            List<CalendarDate> entities = new List<CalendarDate>(batchSize);

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

                    int exceptionId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("exception_type", null), -1);

                    if (!EnumUtil.TryFromValue(exceptionId, out ExceptionType exceptionType))
                    {
                        _logger.LogWarning("Invalid exception type: {ExceptionId}", exceptionId);
                        continue;
                    }

                    string serviceId = rowData.GetValueOrDefault("service_id", "") ?? "";
                    string date = rowData.GetValueOrDefault("date", "") ?? "";
                    string uniqueKey = serviceId.ToLower() + ":" + date.ToLower();
                    if (existingIds.Contains(uniqueKey))
                    {
                        totalIgnored++;
                        continue;
                    }

                    CalendarDate entity = new CalendarDate
                    {
                        Id = Guid.NewGuid().ToString(),
                        ServiceId = serviceId,
                        Date = date,
                        ExceptionType = exceptionType
                    };

                    entities.Add(entity);
                    existingIds.Add(uniqueKey);

                    if (entities.Count >= batchSize)
                    {
                        await _dbContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _dbContext.BulkInsertAsync(entities);
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                $"Inserted {totalImported} records from {filePath} in database with {totalIgnored} line(s) ignored. ({{0}})",
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