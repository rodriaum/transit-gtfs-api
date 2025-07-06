using System.Globalization;
using TransitGtfsApi.Enums;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using TransitGtfsApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace TransitGtfsApi.Service;

public class FareAttributesService : IFareAttributesService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<FareAttributesService> _logger;
    private readonly IRedisService _redis;

    public FareAttributesService(TransitDbContext dbContext, ILogger<FareAttributesService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<FareAttribute>> GetAllAsync()
    {
        return await _dbContext.FareAttributes.ToListAsync();
    }

    public async Task<FareAttribute?> GetByIdAsync(string fareId)
    {
        return await _redis.GetOrSetAsync(
            $"fare-attributes-{fareId}",
            async () => await _dbContext.FareAttributes.FirstOrDefaultAsync(f => f.FareId == fareId)
        );
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "fare_attributes.txt");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return;
        }
        try
        {
            _logger.LogInformation($"Importing data from {filePath}");
            var entities = new List<FareAttribute>();
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
                        rowData[headers[j]] = string.IsNullOrWhiteSpace(values[j]) ? null : values[j];
                }
                int paymentMethodId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("payment_method", null), 0);
                if (!EnumUtil.TryFromValue(paymentMethodId, out PaymentMethodType paymentMethod))
                    paymentMethod = PaymentMethodType.PayBefore;
                var entity = new FareAttribute
                {
                    Id = Guid.NewGuid().ToString(),
                    FareId = rowData.GetValueOrDefault("fare_id", "") ?? "",
                    Price = NumberUtil.ParseDecimalSafe(rowData.GetValueOrDefault("price", null), format: CultureInfo.InvariantCulture),
                    CurrencyType = rowData.GetValueOrDefault("currency_type", "") ?? "",
                    PaymentMethod = paymentMethod,
                    Transfers = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("transfers", null)),
                    TransferDuration = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("transfer_duration", null)),
                };
                entities.Add(entity);
            }
            if (entities.Count > 0)
            {
                _dbContext.FareAttributes.AddRange(entities);
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