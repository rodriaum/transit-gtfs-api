using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TransitGtfsApi.Databases;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Interfaces.Gtfs.Static;
using TransitGtfsApi.Models;
using TransitGtfsApi.Utils;

namespace TransitGtfsApi.Services.Gtfs.Static;

public class CalendarDatesService : ICalendarDatesService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<CalendarDatesService> _logger;
    private readonly IRedisService _redis;

    public CalendarDatesService(TransitDbContext dbContext, ILogger<CalendarDatesService> logger, IRedisService redis)
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
            _logger.LogInformation($"Importing data from {filePath}");

            int batchSize = 1000;
            int totalImported = 0;

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

                    CalendarDate entity = new CalendarDate
                    {
                        Id = Guid.NewGuid().ToString(),
                        ServiceId = rowData.GetValueOrDefault("service_id", "") ?? "",
                        Date = rowData.GetValueOrDefault("date", "") ?? "",
                        ExceptionType = exceptionType
                    };

                    entities.Add(entity);

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

            string duration = TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"Imported {totalImported} records from {filePath} in {duration}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            throw;
        }
    }
}