using TransitGtfsApi.Services.Gtfs;

namespace TransitGtfsApi.Models.Router;

public class AStarNode
{
    public string StopId { get; set; } = null!;
    public DateTime ArrivalTime { get; set; }
    public double G { get; set; }
    public double H { get; set; }
    public List<RouteLeg> Path { get; set; } = new();
}