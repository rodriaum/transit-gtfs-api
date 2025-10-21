using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Tranzor;

public class Constant
{
    public static readonly GeometryFactory Wgs84GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public const string ConfigPath = "Config";
    private const string RootPath = "../Data";

    public static string ExtractPath => Path.Combine(RootPath, "Gtfs", "Normal");
    public static string TempDownloadFolder => Path.GetTempPath();

    public const string Name = "Tranzor";
    public const string Version = "1.0.0";

    public const int SqlBatchSizeImport = 1000;
    public const int CassandraBatchSizeImport = 15;

    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public static readonly string[] RequiredEnvVars =
    [
        "POSTGRES_CONNECTION",
        "REDIS_CONNECTION",
        "POSTGRES_DATABASE_NAME",
        "REDIS_INSTANCE_NAME",
        "API_TOKEN",
        "OTP_URL",
        "CASSANDRA_CONTACT_POINTS",
        "CASSANDRA_KEYSPACE"
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