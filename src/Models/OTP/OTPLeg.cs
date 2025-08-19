namespace Tranzor.Models.OTP;

public class OTPLeg
{
    public string Mode { get; set; }
    public OTPLocation From { get; set; }
    public OTPLocation To { get; set; }
    public OTPRoute Route { get; set; }
    public OTPLegGeometry LegGeometry { get; set; }
}
