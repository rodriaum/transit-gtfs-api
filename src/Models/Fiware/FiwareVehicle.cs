namespace Tranzor.Models.Fiware;

using System.Text.Json.Serialization;
using System.Collections.Generic;

public class FiwareVehicle
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("ambientNoise")]
    public FiwarePropertyValue<double> AmbientNoise { get; set; }

    [JsonPropertyName("annotations")]
    public FiwarePropertyValue<List<string>> Annotations { get; set; }

    [JsonPropertyName("category")]
    public FiwarePropertyValue<List<string>> Category { get; set; }

    [JsonPropertyName("dataProvider")]
    public FiwarePropertyValue<string> DataProvider { get; set; }

    [JsonPropertyName("fleetVehicleId")]
    public FiwarePropertyValue<string> FleetVehicleId { get; set; }

    [JsonPropertyName("heading")]
    public FiwarePropertyValue<double> Heading { get; set; }

    [JsonPropertyName("location")]
    public FiwarePropertyValue<FiwareLocationValue> Location { get; set; }

    [JsonPropertyName("serviceProvided")]
    public FiwarePropertyValue<List<string>> ServiceProvided { get; set; }

    [JsonPropertyName("source")]
    public FiwarePropertyValue<string> Source { get; set; }

    [JsonPropertyName("speed")]
    public FiwarePropertyValue<double> Speed { get; set; }

    [JsonPropertyName("vehicleType")]
    public FiwarePropertyValue<string> VehicleType { get; set; }
}