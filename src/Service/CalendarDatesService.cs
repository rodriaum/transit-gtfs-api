using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using TransitGtfsApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace TransitGtfsApi.Service;

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
        string filePath = Path.Combine(directoryPath, "calendar_dates.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return;
        }

        try
        {
            _logger.LogInformation($"Importing data from {filePath}");
            var entities = new List<CalendarDate>();
            string[] lines = await File.ReadAllLinesAsync(filePath);

            if (lines.Length <= 1)
            {
                _logger.LogWarning($"No data found in {filePath}");
                return;
            }

            string[] headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');
                var rowData = new Dictionary<string, string?>();

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

                var entity = new CalendarDate
                {
                    Id = Guid.NewGuid().ToString(),
                    ServiceId = rowData.GetValueOrDefault("service_id", "") ?? "",
                    Date = rowData.GetValueOrDefault("date", "") ?? "",
                    ExceptionType = exceptionType
                };
                entities.Add(entity);
            }

            if (entities.Count > 0)
            {
                _dbContext.CalendarDates.AddRange(entities);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Imported {entities.Count} records from {filePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            throw;
        }
    }
}