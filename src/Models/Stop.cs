using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TransitGtfsApi.Enums;

namespace TransitGtfsApi.Models;

public class Stop
{
    public string Id { get; set; }
    public string StopId { get; set; }
    public string? StopCode { get; set; }
    public string StopName { get; set; }
    public string? StopDesc { get; set; }
    public double StopLat { get; set; }
    public double StopLon { get; set; }
    public string ZoneId { get; set; }
    public string StopUrl { get; set; }
    public LocationType? LocationType { get; set; }
    public string? ParentStation { get; set; }
    public string? StopTimezone { get; set; }
    public AccessibilityType? WheelchairBoarding { get; set; }
    public string? PlatformCode { get; set; }

    [JsonIgnore]
    public Point? Location { get; set; }

    [NotMapped]
    public long? Distance { get; set; }
    [NotMapped]
    public long? WalkingTime { get; set; }

    public void CalcDistAndWalking(double lat, double lon)
    {
        GeometryFactory geometryFactory = GeometryFactory.Default;
        Point userLocation = geometryFactory.CreatePoint(new Coordinate(lon, lat));

        if (Location == null)
        {
            Location = geometryFactory.CreatePoint(new Coordinate(StopLon, StopLat));
        }

        double distanceInMeters = Location.Distance(userLocation) * 111_000;

        Distance = (long)Math.Round(distanceInMeters);

        WalkingTime = (long)Math.Round(distanceInMeters / 1.4);
    }
}