namespace Tranzor.Models.OTP;

public class OTPNode
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public List<OTPLeg> Legs { get; set; }
}
