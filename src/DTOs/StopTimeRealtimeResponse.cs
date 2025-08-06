using Tranzor.Models;

namespace Tranzor.DTOs;

public class StopTimeRealtimeResponse
{
    public List<StopTimeRealtimeDto> StopTimes { get; set; } = new();
    public bool HasRealtimeData { get; set; }
    public DateTime? LastUpdate { get; set; }
    public string? Source { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int OnTimeCount { get; set; }
    public int DelayedCount { get; set; }
    public int EarlyCount { get; set; }
    public double AverageDelaySeconds { get; set; }

    public static StopTimeRealtimeResponse FromStopTimes(List<StopTime> stopTimes, int page = 1, int pageSize = 100)
    {
        var dtos = stopTimes.Select(StopTimeRealtimeDto.FromStopTime).ToList();
        var hasRealtime = dtos.Any(dto => dto.IsRealtime);

        return new StopTimeRealtimeResponse
        {
            StopTimes = dtos,
            HasRealtimeData = hasRealtime,
            LastUpdate = hasRealtime ? dtos.Where(d => d.LastRealtimeUpdate.HasValue)
                .Max(d => d.LastRealtimeUpdate) : null,
            Source = hasRealtime ? "GTFS-Realtime" : "GTFS-Static",
            TotalCount = dtos.Count,
            Page = page,
            PageSize = pageSize,
            OnTimeCount = dtos.Count(d => d.DelaySeconds == 0),
            DelayedCount = dtos.Count(d => d.DelaySeconds > 0),
            EarlyCount = dtos.Count(d => d.DelaySeconds < 0),
            AverageDelaySeconds = dtos.Where(d => d.DelaySeconds.HasValue)
                .Select(d => d.DelaySeconds.Value).DefaultIfEmpty(0).Average()
        };
    }
}
