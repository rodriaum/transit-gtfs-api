namespace Tranzor.Models.OTP;

public class OTPLocation
{
    public string Name { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public OTPTimeInfo Departure { get; set; }
    public OTPTimeInfo Arrival { get; set; }
}
