namespace Tranzor.Models;

public class FareProduct
{
    public string Id { get; set; }
    public string FareProductId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
}
