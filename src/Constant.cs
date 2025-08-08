using NetTopologySuite;
using NetTopologySuite.Geometries;
using Tranzor.Models;

namespace Tranzor;

public class Constant
{
    public static readonly GeometryFactory GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public const string RootPath = "../Assets";

    public static string ExtractPath => Path.Combine(RootPath, "GtfsData", "Normal");
    public static string ExtractPathRealtime => Path.Combine(RootPath, "GtfsData", "Realtime");
    public static string TempDownloadFolder => Path.Combine(RootPath, "TempGtfs");

    public const string Name = "Tranzor";
    public const string Version = "1.0.0";

    public const string ModeWalkingKey = "walking";
    public const string ModeTransitKey = "transit";

    public const int BatchSizeImport = 1000;

    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public static readonly string[] RequiredEnvVars =
    [
        "POSTGRES_CONNECTION",
        "REDIS_CONNECTION",
        "POSTGRES_DATABASE_NAME",
        "REDIS_INSTANCE_NAME"
    ];

    public static readonly string[] RequiredFiles =
    [
        "agency.txt",
        "stops.txt",
        "routes.txt",
        "trips.txt",
        "stop_times.txt",
        "calendar.txt",
        "calendar_dates.txt"
    ];
}