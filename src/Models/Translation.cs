namespace TransitGtfsApi.Models;

public class Translation
{
    public string Id { get; set; }
    public string TableName { get; set; }
    public string FieldName { get; set; }
    public string Language { get; set; }
    public string TranslationText { get; set; }
    public string? RecordId { get; set; }
    public string? RecordSubId { get; set; }
}
