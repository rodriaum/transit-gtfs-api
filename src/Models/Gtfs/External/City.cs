using NetTopologySuite.Geometries;

namespace Tranzor.Models.Gtfs.External;

public class City
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string CountryCode { get; set; }

    public short? AdminLevel { get; set; }

    public string Source { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public MultiPolygon Geom { get; set; }
}