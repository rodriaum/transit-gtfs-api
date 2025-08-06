using Tranzor.Models;

namespace Tranzor.DTOs;

public class TripWithStopTimesDto
{
    public Trip Trip { get; set; }
    public List<UpcomingDeparturesDto> StopTimes { get; set; }
}