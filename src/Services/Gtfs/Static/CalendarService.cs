using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class CalendarService : ICalendarService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<CalendarService> _logger;
    private readonly IPostgresService _postgresService;

    public CalendarService(GtfsDbContext gtfsDbContext, ILogger<CalendarService> logger,
        IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<Calendar>> GetAllAsync()
    {
        return await _gtfsDbContext.Calendars.ToListAsync();
    }

    public async Task<Calendar?> GetByIdAsync(string serviceId)
    {
        return await _gtfsDbContext.Calendars.FirstOrDefaultAsync(c => c.ServiceId == serviceId);
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "calendar.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Calendars.Select(c => c.ServiceId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvImportUtil.ReadCsvAsync(filePath, _logger);
        List<Calendar> entities = new List<Calendar>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string serviceId = rowData.GetValueOrDefault("service_id", "") ?? "";

            if (existingIds.Contains(serviceId.ToLower()))
            {
                totalIgnored++;
                continue;
            }

            int mondayValue = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("monday", null), 0);
            int tuesdayValue = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("tuesday", null), 0);
            int wednesdayValue = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("wednesday", null), 0);
            int thursdayValue = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("thursday", null), 0);
            int fridayValue = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("friday", null), 0);
            int saturdayValue = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("saturday", null), 0);
            int sundayValue = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("sunday", null), 0);

            if (!EnumUtil.TryFromValue(mondayValue, out StatusType monday)) monday = StatusType.Inactive;
            if (!EnumUtil.TryFromValue(tuesdayValue, out StatusType tuesday)) tuesday = StatusType.Inactive;
            if (!EnumUtil.TryFromValue(wednesdayValue, out StatusType wednesday)) wednesday = StatusType.Inactive;
            if (!EnumUtil.TryFromValue(thursdayValue, out StatusType thursday)) thursday = StatusType.Inactive;
            if (!EnumUtil.TryFromValue(fridayValue, out StatusType friday)) friday = StatusType.Inactive;
            if (!EnumUtil.TryFromValue(saturdayValue, out StatusType saturday)) saturday = StatusType.Inactive;
            if (!EnumUtil.TryFromValue(sundayValue, out StatusType sunday)) sunday = StatusType.Inactive;

            Calendar entity = new Calendar
            {
                Id = Guid.NewGuid().ToString(),
                ServiceId = serviceId,
                Monday = monday,
                Tuesday = tuesday,
                Wednesday = wednesday,
                Thursday = thursday,
                Friday = friday,
                Saturday = saturday,
                Sunday = sunday,
                StartDate = rowData.GetValueOrDefault("start_date", "") ?? "",
                EndDate = rowData.GetValueOrDefault("end_date", "") ?? "",
            };

            entities.Add(entity);
            existingIds.Add(serviceId.ToLower());

            await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
        }
    }
}