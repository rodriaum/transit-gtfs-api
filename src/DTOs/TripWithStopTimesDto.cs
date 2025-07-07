using TransitGtfsApi.Models;

namespace TransitGtfsApi.DTOs;

public class TripWithStopTimesDto
{
    public Trip Trip { get; set; }
    public List<StopTimeWithStopDto> StopTimes { get; set; }
}