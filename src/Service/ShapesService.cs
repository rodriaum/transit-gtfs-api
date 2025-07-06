using System.Globalization;
using TransitGtfsApi.Interfaces;
using TransitGtfsApi.Interfaces.Database;
using TransitGtfsApi.Models;
using TransitGtfsApi.Service.Database;
using TransitGtfsApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace TransitGtfsApi.Service;

public class ShapesService : IShapesService
{
    private readonly TransitDbContext _dbContext;
    private readonly ILogger<ShapesService> _logger;
    private readonly IRedisService _redis;

    public ShapesService(TransitDbContext dbContext, ILogger<ShapesService> logger, IRedisService redis)
    {
        _dbContext = dbContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Shape>> GetAllAsync()
    {
        return await _dbContext.Shapes.ToListAsync();
    }

    public async Task<List<Shape>?> GetByShapeIdAsync(string shapeId)
    {
        return await _redis.GetOrSetAsync(
            $"shapes-{shapeId}",
            async () => await _dbContext.Shapes.Where(s => s.ShapeId == shapeId).ToListAsync()
        );
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "shapes.txt");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }
        try
        {
            _logger.LogInformation($"Importing data from {filePath}");
            var entities = new List<Shape>();
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
                var entity = new Shape
                {
                    Id = Guid.NewGuid().ToString(),
                    ShapeId = rowData.GetValueOrDefault("shape_id", "") ?? "",
                    ShapePtLat = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_pt_lat", null), format: CultureInfo.InvariantCulture),
                    ShapePtLon = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_pt_lon", null), format: CultureInfo.InvariantCulture),
                    ShapePtSequence = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("shape_pt_sequence", null)),
                    ShapeDistTraveled = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_dist_traveled", null), format: CultureInfo.InvariantCulture),
                };
                entities.Add(entity);
            }
            if (entities.Count > 0)
            {
                _dbContext.Shapes.AddRange(entities);
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