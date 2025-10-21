using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class CalendarDatesService : ICalendarDatesService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<CalendarDatesService> _logger;
    private readonly IPostgresService _postgresService;

    public CalendarDatesService(GtfsDbContext gtfsDbContext, ILogger<CalendarDatesService> logger, IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<CalendarDate>> GetAllAsync()
    {
        return await _gtfsDbContext.CalendarDates.ToListAsync();
    }

    public async Task<List<CalendarDate>?> GetByServiceIdAsync(string serviceId)
    {
        return await _gtfsDbContext.CalendarDates.Where(c => c.ServiceId == serviceId).ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "calendar_dates.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.CalendarDates.Select(c => c.ServiceId.ToLower() + ":" + c.Date.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<CalendarDate> entities = new List<CalendarDate>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            int exceptionId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("exception_type", null), -1);

            if (!EnumUtil.TryFromValue(exceptionId, out ExceptionType exceptionType))
            {
                _logger.LogWarning("Invalid exception type: {ExceptionId}", exceptionId);
                totalIgnored++;
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
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}