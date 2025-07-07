using TransitGtfsApi.Models;

namespace TransitGtfsApi.DTOs;

public class StopTimeWithStopDto
{
    public StopTime StopTime { get; set; }
    public Stop Stop { get; set; }
}