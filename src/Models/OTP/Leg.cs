namespace Tranzor.Models.OTP;

public class Leg
{
    public string Mode { get; set; }
    public Location From { get; set; }
    public Location To { get; set; }
    public RouteOTP? Route { get; set; }
    public LegGeometry? LegGeometry { get; set; }
}