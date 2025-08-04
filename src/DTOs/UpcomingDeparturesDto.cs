using TransitGtfsApi.Models;

namespace TransitGtfsApi.DTOs;

public class UpcomingDeparturesDto
{
    public StopTime StopTime { get; set; }
    public Stop Stop { get; set; }
    public Trip? Trip { get; set; }
    public Models.Route? Route { get; set; }
}