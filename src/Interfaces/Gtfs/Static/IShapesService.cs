using TransitGtfsApi.Models;

namespace TransitGtfsApi.Interfaces.Gtfs.Static;

public interface IShapesService
{
    Task<List<Shape>> GetAllAsync();
    Task<List<Shape>?> GetByShapeIdAsync(string shapeId);
    Task ImportDataAsync(string directoryPath);
}
