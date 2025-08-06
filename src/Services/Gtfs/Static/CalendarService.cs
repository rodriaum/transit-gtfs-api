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

public class CalendarService : ICalendarService
{
    private readonly GTFSContext _dbContext;
    private readonly ILogger<CalendarService> _logger;
    private readonly IRedisService _redis;

    public CalendarService(GTFSContext dbContext, ILogger<CalendarService> logger, IRedisService redis)
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
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "calendar.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.BatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingIds = new(
                await _dbContext.Calendars.Select(c => c.ServiceId.ToLower()).ToListAsync()
            );

            List<Calendar> entities = new List<Calendar>(batchSize);

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
                            rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
                    }
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