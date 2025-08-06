using NetTopologySuite.Geometries;

namespace Tranzor.Models;

public class Shape
{
    public string Id { get; set; }
    public string ShapeId { get; set; }
    public double ShapePtLat { get; set; }
    public double ShapePtLon { get; set; }
    public int ShapePtSequence { get; set; }
    public double? ShapeDistTraveled { get; set; }

    public Point? Geom { get; set; }
}