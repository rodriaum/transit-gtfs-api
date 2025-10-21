using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Interfaces.Gtfs.Static;
using Tranzor.Models;
using Tranzor.Utils;

namespace Tranzor.Services.Gtfs.Static;

public class ShapesService : IShapesService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<ShapesService> _logger;
    private readonly IPostgresService _postgresService;

    public ShapesService(GtfsDbContext gtfsDbContext, ILogger<ShapesService> logger,
        IPostgresService postgresService)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
        _postgresService = postgresService;
    }

    public async Task<List<Shape>> GetAllAsync()
    {
        return await _gtfsDbContext.Shapes.ToListAsync();
    }

    public async Task<List<Shape>?> GetByShapeIdAsync(string shapeId)
    {
        return await _gtfsDbContext.Shapes.Where(s => s.ShapeId == shapeId).ToListAsync();
    }

    public async Task ImportDataAsync(string directoryPath)
    {
        string filePath = Path.Combine(directoryPath, "shapes.txt");

        HashSet<string> existingIds = new(
            await _gtfsDbContext.Shapes.Select(s => s.ShapeId.ToLower()).ToListAsync()
        );

        List<Dictionary<string, string?>> csvData = await CsvUtil.ReadCsvAsync(filePath, _logger);
        List<Shape> entities = new List<Shape>();
        int totalIgnored = 0;

        foreach (var rowData in csvData)
        {
            string shapeId = rowData.GetValueOrDefault("shape_id", "") ?? "";

            if (existingIds.Contains(shapeId.ToLower()))
            {
                totalIgnored++;
                continue;
            }

            double shapeLat = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_pt_lat", null),
                format: CultureInfo.InvariantCulture);
            double shapeLon = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_pt_lon", null),
                format: CultureInfo.InvariantCulture);

            Shape entity = new Shape
            {
                Id = Guid.NewGuid().ToString(),
                ShapeId = shapeId,
                ShapePtLat = shapeLat,
                ShapePtLon = shapeLon,
                ShapePtSequence = NumberUtil.ParseIntSafe(rowData.GetValueOrDefault("shape_pt_sequence", null)),
                ShapeDistTraveled = NumberUtil.ParseDoubleSafe(rowData.GetValueOrDefault("shape_dist_traveled", null),
                    format: CultureInfo.InvariantCulture),
                Geom = Constant.Wgs84GeometryFactory.CreatePoint(new Coordinate(shapeLon, shapeLat))
            };

            entities.Add(entity);
            existingIds.Add(shapeId.ToLower());
        }

        await _postgresService.BulkInsertEntitiesAsync(entities, filePath, totalIgnored);
    }
}