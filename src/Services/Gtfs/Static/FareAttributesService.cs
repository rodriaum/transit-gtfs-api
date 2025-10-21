using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Enums;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FareAttributesService : IFareAttributesService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<FareAttributesService> _logger;
    private readonly IPostgresService _postgresService;

    public FareAttributesService(GtfsDbContext gtfsDbContext, ILogger<FareAttributesService> logger, IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<FareAttribute>> GetAllAsync()
    {
        return await _gtfsDbContext.FareAttributes.ToListAsync();
    }

    public async Task<FareAttribute?> GetByIdAsync(string fareId)
    {
        return await _gtfsDbContext.FareAttributes.FirstOrDefaultAsync(f => f.FareId == fareId);
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "fare_attributes.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.FareAttributes.Select(f => f.FareId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<FareAttribute> entities = new List<FareAttribute>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string fareId = rowData.GetValueOrDefault("fare_id", "") ?? "";

            if (existingIds.Contains(fareId.ToLower()))
            {
                totalIgnored++;
                continue;
            }

            int paymentMethodId = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("payment_method", null), 0);
            if (!EnumUtil.TryFromValue(paymentMethodId, out PaymentMethodType paymentMethod))
            {
                paymentMethod = PaymentMethodType.PayBefore;
            }

            FareAttribute entity = new FareAttribute
            {
                Id = Guid.NewGuid().ToString(),
                FareId = fareId,
                Price = NumberUtil.ParseDecimalSafe(rowData.GetValueOrDefault("price", null),
                    format: CultureInfo.InvariantCulture),
                CurrencyType = rowData.GetValueOrDefault("currency_type", "") ?? "",
                PaymentMethod = paymentMethod,
                Transfers = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("transfers", null)),
                TransferDuration = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("transfer_duration", null)),
            };

            entities.Add(entity);
            existingIds.Add(fareId.ToLower());
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}