namespace Tranzor.Models.OTP;

public class RoutePlan
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public List<Leg> Legs { get; set; } = new();
}