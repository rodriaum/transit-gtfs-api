using System.Globalization;
using System.Net;
using System.Text.Json;
using TransitRealtime;
using Tranzor.Models.Fiware;

namespace Tranzor.Utils;

public class ConverterUtil
{
    /// <summary>
    /// Converts a collection of FiwareVehicle objects to a GTFS-RT FeedMessage.
    /// </summary>
    public static FeedMessage ConvertToGtfsRealtimeFeed(IEnumerable<FiwareVehicle> fiwareVehicles)
    {
        FeedMessage message = new FeedMessage
        {
            Header = new FeedHeader
            {
                GtfsRealtimeVersion = "2.0",
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };

        IEnumerable<FeedEntity> entities = fiwareVehicles
            .Select(CreateFeedEntityFromFiwareVehicle)
            .Where(entity => entity != null)!;

        message.Entity.AddRange(entities);

        return message;
    }

    /// <summary>
    /// Creates a single FeedEntity from a FiwareVehicle object.
    /// </summary>
    private static FeedEntity? CreateFeedEntityFromFiwareVehicle(FiwareVehicle vehicle)
    {
        if (string.IsNullOrEmpty(vehicle.Id) || vehicle.Location?.Value?.Coordinates?.Count < 2)
        {
            return null;
        }

        VehiclePosition vehiclePosition = new VehiclePosition();

        vehiclePosition.Vehicle = new VehicleDescriptor { Id = vehicle.Id };

        Position position = new Position
        {
            Longitude = (float)vehicle.Location.Value.Coordinates[0],
            Latitude = (float)vehicle.Location.Value.Coordinates[1]
        };

        if (vehicle.Speed?.Value != null)
        {
            position.Speed = (float)(vehicle.Speed.Value * (1000.0 / 3600.0));
        }

        if (vehicle.Heading?.Value != null)
        {
            position.Bearing = (float)vehicle.Heading.Value;
        }

        vehiclePosition.Position = position;

        if (vehicle.Location?.Metadata != null && vehicle.Location.Metadata.TryGetValue("timestamp", out object? timestampObject))
        {
            if (timestampObject is JsonElement timestampElement &&
                timestampElement.TryGetProperty("value", out JsonElement valueElement) &&
                valueElement.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(valueElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime timestamp))
            {
                vehiclePosition.Timestamp = (ulong)new DateTimeOffset(timestamp).ToUnixTimeSeconds();
            }
        }

        bool hasTrip = false;

        TripDescriptor tripDescriptor = new TripDescriptor();

        // NOTE: Different agencies have different ways of doing TripId, so it will be really hardcore.
        // FIXME: Maybe it will be modified in the future.
        switch (vehicle.DataProvider.Value.ToLower())
        {
            case "stcp":
                List<string> annotations = vehicle.Annotations.Value;

                if (annotations != null && annotations.Any())
                {
                    string[] strings = annotations.ToArray();

                    string GetValue(string prefix) =>
                        WebUtility.UrlDecode(annotations.First(v => v.StartsWith(prefix)))
                        .Split(':')
                        .Last();

                    string route = GetValue("stcp%3Aroute%3A");
                    string turn = GetValue("stcp%3Anr_turno%3A");
                    string trip = GetValue("stcp%3Anr_viagem%3A");
                    string sense = GetValue("stcp%3Asentido%3A");

                    string type = "U";

                    tripDescriptor.TripId = $"{route}_{turn}_{type}_{sense}";
                    hasTrip = true;
                }
                break;
        }

        TripUpdate? tripUpdate = new TripUpdate();
        tripUpdate.Trip = tripDescriptor;

        if (!hasTrip)
            tripUpdate = null;

        return new FeedEntity
        {
            Id = vehicle.Id,
            Vehicle = vehiclePosition,
            TripUpdate = tripUpdate
        };
    }
}