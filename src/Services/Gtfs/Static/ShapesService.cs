using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Diagnostics;
using System.Globalization;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class ShapesService : IShapesService
{
    private readonly GtfsDbContext _gtfsDBContext;
    private readonly ILogger<ShapesService> _logger;
    private readonly IRedisService _redis;

    public ShapesService(GtfsDbContext gtfsDBContext, ILogger<ShapesService> logger, IRedisService redis)
    {
        _gtfsDBContext = gtfsDBContext;
        _logger = logger;
        _redis = redis;
    }

    public async Task<List<Shape>> GetAllAsync()
    {
        return await _gtfsDBContext.Shapes.ToListAsync();
    }

    public async Task<List<Shape>?> GetByShapeIdAsync(string shapeId)
    {
        return await _redis.GetOrSetAsync(
            $"shapes-{shapeId}",
            async () => await _gtfsDBContext.Shapes.Where(s => s.ShapeId == shapeId).ToListAsync()
        );
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string filePath = Path.Combine(directoryPath, "shapes.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return;
        }

        try
        {
            _logger.LogInformation($"Starting data import process from {filePath}");

            int batchSize = Constant.SqlBatchSizeImport;
            int totalImported = 0;
            int totalIgnored = 0;

            HashSet<string> existingIds = new HashSet<string>(
                await _gtfsDBContext.Shapes.Select(s => s.ShapeId.ToLower()).ToListAsync()
            );

            List<Shape> entities = new List<Shape>(batchSize);

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

                    string shapeId = rowData.GetValueOrDefault("shape_id", "") ?? "";
                    if (existingIds.Contains(shapeId.ToLower()))
                    {
                        totalIgnored++;
                        continue;
                    }

                    double shapeLat = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_pt_lat", null), format: CultureInfo.InvariantCulture);
                    double shapeLon = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_pt_lon", null), format: CultureInfo.InvariantCulture);

                    Shape entity = new Shape
                    {
                        Id = Guid.NewGuid().ToString(),
                        ShapeId = shapeId,
                        ShapePtLat = shapeLat,
                        ShapePtLon = shapeLon,
                        ShapePtSequence = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("shape_pt_sequence", null)),
                        ShapeDistTraveled = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_dist_traveled", null), format: CultureInfo.InvariantCulture),
                        Geom = Constant.Wgs84GeometryFactory.CreatePoint(new Coordinate(shapeLon, shapeLat))
                    };

                    entities.Add(entity);
                    existingIds.Add(shapeId.ToLower());

                    if (entities.Count >= batchSize)
                    {
                        await _gtfsDBContext.BulkInsertAsync(entities);
                        totalImported += entities.Count;
                        entities.Clear();
                    }
                }

                if (entities.Count > 0)
                {
                    await _gtfsDBContext.BulkInsertAsync(entities);
                    totalImported += entities.Count;
                    entities.Clear();
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Inserted {0} records from {1} in database with {2} line(s) ignored. ({3})",
                totalImported,
                filePath,
                totalIgnored,
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