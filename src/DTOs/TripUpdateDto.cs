namespace Tranzor.DTOs;

public class TripUpdateDto
{
    public string? TripId { get; set; }
    public string? RouteId { get; set; }
    public string? VehicleId { get; set; }
    public ulong? Timestamp { get; set; }

    public TripUpdateDto(TransitRealtime.TripUpdate update)
    {
        TripId = update.Trip?.TripId;
        RouteId = update.Trip?.RouteId;
        VehicleId = update.Vehicle?.Id;
        Timestamp = update.HasTimestamp ? update.Timestamp : null;
    }
}