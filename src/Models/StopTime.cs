using System.ComponentModel.DataAnnotations.Schema;
using TransitGtfsApi.Enums;

namespace TransitGtfsApi.Models;
public class StopTime
{
    public string Id { get; set; }
    public string TripId { get; set; }
    public string ArrivalTime { get; set; }
    public string DepartureTime { get; set; }
    public string StopId { get; set; }
    public int StopSequence { get; set; }
    public string? StopHeadsign { get; set; }
    public PickupType? PickupType { get; set; }
    public int? DropOffType { get; set; }
    public double? ShapeDistTraveled { get; set; }

    public TimepointType? Timepoint { get; set; }

    [NotMapped]
    public DateTime? RealtimeArrival { get; set; }
    [NotMapped]
    public DateTime? RealtimeDeparture { get; set; }
    [NotMapped]
    public int? DelaySeconds { get; set; }
    [NotMapped]
    public bool IsRealtime { get; set; }
    [NotMapped]
    public double? DistanceToStop { get; set; }
    [NotMapped]
    public double? VehicleSpeed { get; set; }
    [NotMapped]
    public DateTime? LastRealtimeUpdate { get; set; }
    [NotMapped]
    public string? RealtimeStatus { get; set; }
    [NotMapped]
    public string EffectiveArrivalTime =>
        RealtimeArrival?.ToString("HH:mm:ss") ?? ArrivalTime;
    [NotMapped]
    public string EffectiveDepartureTime =>
        RealtimeDeparture?.ToString("HH:mm:ss") ?? DepartureTime;
    [NotMapped]
    public bool HasSignificantDelay => DelaySeconds > 120;
    [NotMapped]
    public string DelayText
    {
        get
        {
            if (!DelaySeconds.HasValue) return "No delay info";

            var delay = DelaySeconds.Value;
            if (delay == 0) return "On time";
            if (delay > 0) return $"{delay / 60}m {delay % 60}s late";
            return $"{Math.Abs(delay) / 60}m {Math.Abs(delay) % 60}s early";
        }
    }
}