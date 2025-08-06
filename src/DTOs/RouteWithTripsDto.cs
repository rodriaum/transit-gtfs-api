using Tranzor.Models;

namespace Tranzor.DTOs;

public class RouteWithTripsDto
{
    public Models.Route Route { get; set; }
    public List<Trip> Trips { get; set; }
}