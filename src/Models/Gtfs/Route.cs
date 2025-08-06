using TransitGtfsApi.Enums;

namespace TransitGtfsApi.Models;
public class Route
{
    public string Id { get; set; }
    public string RouteId { get; set; }
    public string? AgencyId { get; set; }
    public string RouteShortName { get; set; }
    public string RouteLongName { get; set; }
    public string? RouteDesc { get; set; }
    public RouteType RouteType { get; set; } 
    public string? RouteUrl { get; set; }
    public string? RouteColor { get; set; }
    public string? RouteTextColor { get; set; }
    public int? RouteSortOrder { get; set; }
    public int? ContinuousPickup { get; set; }
    public int? ContinuousDropOff { get; set; }
}