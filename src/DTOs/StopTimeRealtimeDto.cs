using TransitGtfsApi.Models;

namespace TransitGtfsApi.DTOs;

public class StopTimeRealtimeDto
{
    public string Id { get; set; } = string.Empty;
    public string TripId { get; set; } = string.Empty;
    public string StopId { get; set; } = string.Empty;
    public int StopSequence { get; set; }
    public string? StopHeadsign { get; set; }

    public string ScheduledArrivalTime { get; set; } = string.Empty;
    public string ScheduledDepartureTime { get; set; } = string.Empty;

    public DateTime? RealtimeArrival { get; set; }
    public DateTime? RealtimeDeparture { get; set; }
    public string? RealtimeArrivalTime { get; set; }
    public string? RealtimeDepartureTime { get; set; }

    public int? DelaySeconds { get; set; }
    public string DelayText { get; set; } = string.Empty;
    public bool HasSignificantDelay { get; set; }

    public bool IsRealtime { get; set; }
    public double? DistanceToStop { get; set; }
    public double? VehicleSpeed { get; set; }
    public DateTime? LastRealtimeUpdate { get; set; }
    public string? RealtimeStatus { get; set; }

    public object? PickupType { get; set; }
    public object? DropOffType { get; set; }
    public double? ShapeDistTraveled { get; set; }
    public object? Timepoint { get; set; }

    public string EffectiveArrivalTime { get; set; } = string.Empty;
    public string EffectiveDepartureTime { get; set; } = string.Empty;

    public string? VehicleId { get; set; }
    public string? VehicleLabel { get; set; }
    public double? VehicleLatitude { get; set; }
    public double? VehicleLongitude { get; set; }
    public DateTime? VehicleTimestamp { get; set; }

    public int? EtaMinutes { get; set; }

    public string Status { get; set; } = "Scheduled";

    public static StopTimeRealtimeDto FromStopTime(StopTime stopTime)
    {
        return new StopTimeRealtimeDto
        {
            Id = stopTime.Id,
            TripId = stopTime.TripId,
            StopId = stopTime.StopId,
            StopSequence = stopTime.StopSequence,
            StopHeadsign = stopTime.StopHeadsign,
            ScheduledArrivalTime = stopTime.ArrivalTime,
            ScheduledDepartureTime = stopTime.DepartureTime,
            RealtimeArrival = stopTime.RealtimeArrival,
            RealtimeDeparture = stopTime.RealtimeDeparture,
            RealtimeArrivalTime = stopTime.RealtimeArrival?.ToString("HH:mm:ss"),
            RealtimeDepartureTime = stopTime.RealtimeDeparture?.ToString("HH:mm:ss"),
            DelaySeconds = stopTime.DelaySeconds,
            DelayText = stopTime.DelayText,
            HasSignificantDelay = stopTime.HasSignificantDelay,
            IsRealtime = stopTime.IsRealtime,
            DistanceToStop = stopTime.DistanceToStop,
            VehicleSpeed = stopTime.VehicleSpeed,
            LastRealtimeUpdate = stopTime.LastRealtimeUpdate,
            RealtimeStatus = stopTime.RealtimeStatus,
            PickupType = stopTime.PickupType,
            DropOffType = stopTime.DropOffType,
            ShapeDistTraveled = stopTime.ShapeDistTraveled,
            Timepoint = stopTime.Timepoint,
            EffectiveArrivalTime = stopTime.EffectiveArrivalTime,
            EffectiveDepartureTime = stopTime.EffectiveDepartureTime,
            EtaMinutes = stopTime.RealtimeArrival.HasValue ?
                (int?)Math.Max(0, (stopTime.RealtimeArrival.Value - DateTime.UtcNow).TotalMinutes) : null,
            Status = stopTime.IsRealtime ? "Realtime" : "Scheduled"
        };
    }
}