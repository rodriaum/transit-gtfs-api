using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class FareProductService : IFareProductService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<FareProductService> _logger;
    private readonly IPostgresService _postgresService;

    public FareProductService(GtfsDbContext gtfsDbContext, ILogger<FareProductService> logger,
        IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<FareProduct>> GetAllAsync()
    {
        return await _gtfsDbContext.Set<FareProduct>().ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "fare_products.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Set<FareProduct>().Select(f => f.FareProductId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvImportUtil.ReadCsvAsync(filePath, _logger);
        List<FareProduct> entities = new List<FareProduct>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string fareProductId = rowData.GetValueOrDefault("fare_product_id", "") ?? "";
            string uniqueKey = fareProductId.ToLower();

            if (existingIds.Contains(uniqueKey))
            {
                totalIgnored++;
                continue;
            }

            FareProduct entity = new FareProduct
            {
                Id = Guid.NewGuid().ToString(),
                FareProductId = fareProductId,
                Name = rowData.GetValueOrDefault("name", "") ?? "",
                Description = rowData.GetValueOrDefault("description", null),
                Type = rowData.GetValueOrDefault("type", null),
                Amount = decimal.TryParse(rowData.GetValueOrDefault("amount", null), out var amount) ? amount : null,
                Currency = rowData.GetValueOrDefault("currency", null)
            };

            entities.Add(entity);
            existingIds.Add(uniqueKey);
            await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
        }
    }
}