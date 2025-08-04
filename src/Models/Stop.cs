using NetTopologySuite.Geometries;
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
}