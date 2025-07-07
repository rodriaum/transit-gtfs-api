using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Utils;
using Microsoft.EntityFrameworkCore;
using TransitGtfsApi.Databases;

namespace TransitGtfsApi.Services.Gtfs.Static;

public class CalendarService : ICalendarService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<CalendarService> _logger;
    private readonly IRedisService _redis;

    public CalendarService(TransitDbContext dbContext, ILogger<CalendarService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Calendar>> GetAllAsync()
    {
        return await _dbContext.Calendars.ToListAsync();
    }

    public async Task<Calendar?> GetByIdAsync(string serviceId)
    {
        return await _redis.GetOrSetAsync(
            $"calendar-{serviceId}",
            async () => await _dbContext.Calendars.FirstOrDefaultAsync(c => c.ServiceId == serviceId)
        );
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "calendar.txt");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }
        try
        {
            _logger.LogInformation($"Importing data from {filePath}");
            int batchSize = 1000;
            var entities = new List<Calendar>(batchSize);
            int totalImported = 0;
            using (var reader = new StreamReader(filePath))
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
                    var rowData = new Dictionary<string, string?>();
                    for (int j = 0; j < headers.Length; j++)
                    {
                        if (j < values.Length)
                            rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
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
                    var entity = new Calendar
                    {
                        Id = Guid.NewGuid().ToString(),
                        ServiceId = rowData.GetValueOrDefault("service_id", "") ?? "",
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
                    if (entities.Count >= batchSize)
                    {
                        _dbContext.Calendars.AddRange(entities);
                        await _dbContext.SaveChangesAsync();
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }
                // Salva o restante
                if (entities.Count > 0)
                {
                    _dbContext.Calendars.AddRange(entities);
                    await _dbContext.SaveChangesAsync();
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }
            _logger.LogInformation($"Imported {totalImported} records from {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"\nError importing data from {filePath}");
            throw;
        }
    }
}