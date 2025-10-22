namespace Tranzor.Services.OTP;

using System.Collections.Generic;
using System.Globalization;
using Tranzor.Interfaces.Gtfs;
using Tranzor.Interfaces.Http;
using Tranzor.Models.OTP;

public class OpenTripPlannerService : IOpenTripPlannerService
{
    private readonly IOtpHttpClient _otpClient;
    private readonly ILogger<OpenTripPlannerService> _logger;

    public OpenTripPlannerService(IOtpHttpClient otpClient, ILogger<OpenTripPlannerService> logger)
    {
        _otpClient = otpClient;
        _logger = logger;
    }

    public async Task<List<RoutePlan>> PlanRouteAsync(double fromLat, double fromLon, double toLat, double toLon, DateTime departureTime, int maxRoutes)
    {
        try
        {
            string query = BuildGraphQLQuery(fromLat, fromLon, toLat, toLon, departureTime);
            OTPResponse? otpResponse = await _otpClient.ExecuteGraphQLQueryAsync(query, "planConnection");

            if (otpResponse == null)
            {
                _logger.LogWarning("No response received from OTP");
                return new List<RoutePlan>();
            }

            return MapOTPResponseToRoutePlans(otpResponse, maxRoutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error planning route from ({FromLat}, {FromLon}) to ({ToLat}, {ToLon})",
                fromLat, fromLon, toLat, toLon);
            throw;
        }
    }

    private string BuildGraphQLQuery(double fromLat, double fromLon, double toLat, double toLon, DateTime departureTime)
    {
        // ISO 8601
        string dateTimeString = departureTime.ToString("yyyy-MM-ddTHH:mm:sszzz");

        return $@"
        query planConnection {{
            planConnection(
                origin: {{
                    location: {{ coordinate: {{ latitude: {fromLat.ToString(CultureInfo.InvariantCulture)}, longitude: {fromLon.ToString(CultureInfo.InvariantCulture)} }} }}
                }}
                destination: {{
                    location: {{ coordinate: {{ latitude: {toLat.ToString(CultureInfo.InvariantCulture)}, longitude: {toLon.ToString(CultureInfo.InvariantCulture)} }} }}
                }}
                dateTime: {{ earliestDeparture: ""{dateTimeString}"" }}
                modes: {{
                    direct: [WALK]
                    transit: {{ transit: [{{ mode: BUS }}, {{ mode: RAIL }}] }}
                }}
            ) {{
                edges {{
                    node {{
                        start
                        end
                        legs {{
                            mode
                            from {{
                                name
                                lat
                                lon
                                departure {{
                                    scheduledTime
                                    estimated {{
                                        time
                                        delay
                                    }}
                                }}
                            }}
                            to {{
                                name
                                lat
                                lon
                                arrival {{
                                    scheduledTime
                                    estimated {{
                                        time
                                        delay
                                    }}
                                }}
                            }}
                            route {{
                                gtfsId
                                longName
                                shortName
                            }}
                            legGeometry {{
                                points
                            }}
                        }}
                    }}
                }}
            }}
        }}";
    }

    private List<RoutePlan> MapOTPResponseToRoutePlans(OTPResponse otpResponse, int maxRoutes)
    {
        if (otpResponse?.Data?.PlanConnection?.Edges == null)
        {
            return new List<RoutePlan>();
        }

        List<RoutePlan> routePlans = new();

        foreach (Edge? edge in otpResponse.Data.PlanConnection.Edges.Take(maxRoutes))
        {
            RoutePlan routePlan = new RoutePlan
            {
                Start = edge.Node.Start,
                End = edge.Node.End,
                Legs = edge.Node.Legs.Select(leg => new Leg
                {
                    Mode = leg.Mode,
                    From = new Location
                    {
                        Name = leg.From.Name,
                        Lat = leg.From.Lat,
                        Lon = leg.From.Lon,
                        Departure = leg.From.Departure != null ? new TimeInfo
                        {
                            ScheduledTime = leg.From.Departure.ScheduledTime,
                            Estimated = leg.From.Departure.Estimated != null ? new EstimatedInfo
                            {
                                Time = leg.From.Departure.Estimated.Time ?? DateTime.MinValue,
                                Delay = leg.From.Departure.Estimated.Delay ?? 0
                            } : null
                        } : null,
                        Arrival = leg.From.Arrival != null ? new TimeInfo
                        {
                            ScheduledTime = leg.From.Arrival.ScheduledTime,
                            Estimated = leg.From.Arrival.Estimated != null ? new EstimatedInfo
                            {
                                Time = leg.From.Arrival.Estimated.Time ?? DateTime.MinValue,
                                Delay = leg.From.Arrival.Estimated.Delay ?? 0
                            } : null
                        } : null
                    },
                    To = new Location
                    {
                        Name = leg.To.Name,
                        Lat = leg.To.Lat,
                        Lon = leg.To.Lon,
                        Departure = leg.To.Departure != null ? new TimeInfo
                        {
                            ScheduledTime = leg.To.Departure.ScheduledTime,
                            Estimated = leg.To.Departure.Estimated != null ? new EstimatedInfo
                            {
                                Time = leg.To.Departure.Estimated.Time ?? DateTime.MinValue,
                                Delay = leg.To.Departure.Estimated.Delay ?? 0
                            } : null
                        } : null,
                        Arrival = leg.To.Arrival != null ? new TimeInfo
                        {
                            ScheduledTime = leg.To.Arrival.ScheduledTime,
                            Estimated = leg.To.Arrival.Estimated != null ? new EstimatedInfo
                            {
                                Time = leg.To.Arrival.Estimated.Time ?? DateTime.MinValue,
                                Delay = leg.To.Arrival.Estimated.Delay ?? 0
                            } : null
                        } : null
                    },
                    Route = leg.Route != null ? new RouteOTP
                    {
                        GtfsId = leg.Route.GtfsId,
                        LongName = leg.Route.LongName,
                        ShortName = leg.Route.ShortName
                    } : null,
                    LegGeometry = leg.LegGeometry != null ? new LegGeometry
                    {
                        Points = leg.LegGeometry.Points
                    } : null
                }).ToList()
            };

            routePlans.Add(routePlan);
        }

        return routePlans;
    }
}