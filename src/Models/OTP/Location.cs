namespace Tranzor.Models.OTP;

public class Location
{
    public string Name { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public TimeInfo? Departure { get; set; }
    public TimeInfo? Arrival { get; set; }
}
