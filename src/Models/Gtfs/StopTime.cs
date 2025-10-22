using Tranzor.Enums;
using Tranzor.Utils;

namespace Tranzor.Models;

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

    public DateTime? RealtimeArrival { get; set; }
    public DateTime? RealtimeDeparture { get; set; }
    public int? DelaySeconds { get; set; }
    public bool IsRealtime { get; set; }
    public double? DistanceToStop { get; set; }
    public double? VehicleSpeed { get; set; }
    public DateTime? LastRealtimeUpdate { get; set; }
    public string? RealtimeStatus { get; set; }

    public string EffectiveArrivalTime =>
        RealtimeArrival?.ToString("HH:mm:ss") ?? ArrivalTime;

    public string EffectiveDepartureTime =>
        RealtimeDeparture?.ToString("HH:mm:ss") ?? DepartureTime;

    public bool HasSignificantDelay => DelaySeconds > 120;

    public string DelayText
    {
        get
        {
            if (!DelaySeconds.HasValue) return "Sem informação de atraso";

            int delay = DelaySeconds.Value;
            if (delay == 0) return "A tempo";
            if (delay > 0) return $"Atrasado {delay / 60}m {delay % 60}s";
            return $"Adiantado {Math.Abs(delay) / 60}m {Math.Abs(delay) % 60}s";
        }
    }

    public TimeSpan ArrivalTimeSpan =>
        TimeFormatUtil.ParseGtfsTime(ArrivalTime);

    public TimeSpan DepartureTimeSpan =>
        TimeFormatUtil.ParseGtfsTime(DepartureTime);
}