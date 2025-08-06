namespace Tranzor.Models.Router;

public class RoutePlan
{
    public TimeSpan Duration { get; set; }
    public List<RouteLeg> Legs { get; set; } = new();
}