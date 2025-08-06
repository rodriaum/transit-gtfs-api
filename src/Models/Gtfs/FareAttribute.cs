using Tranzor.Enums;

namespace Tranzor.Models;

public class FareAttribute
{
    public string Id { get; set; }
    public string FareId { get; set; }
    public decimal? Price { get; set; }
    public string CurrencyType { get; set; }
    public PaymentMethodType PaymentMethod { get; set; }
    public int Transfers { get; set; }
    public int? TransferDuration { get; set; }
}
