using TransitGtfsApi.Enums;
using TransitGtfsApi.Models;

namespace TransitGtfsApi;

public class Constant
{
    public static readonly List<GtfsData> GtfsDataList = new List<GtfsData>
    {
        new GtfsData("metro_porto", "https://www.metrodoporto.pt/metrodoporto/uploads/document/file/693/google_transit_v2.zip"),
        new GtfsData("stcp", "https://opendata.porto.digital/dataset/5275c986-592c-43f5-8f87-aabbd4e4f3a4/resource/89a6854f-2ea3-4ba0-8d2f-6558a9df2a98/download/horarios_gtfs_stcp_16_04_2025.zip"),
        new GtfsData("unir", "https://drive.google.com/uc?export=download&id=1kzV7fKlnL6ZFwd7enOnWR2AZ7yo1eVXa"),
        new GtfsData("cp", "https://publico.cp.pt/gtfs/gtfs.zip"),
        new GtfsData("carris_metropolitana", "https://api.carrismetropolitana.pt/gtfs", null,
            new Dictionary<RealtimeType, string>
            {
                [RealtimeType.VehiclePositions] = "http://api.carrismetropolitana.pt/vehicles.pb",
                [RealtimeType.ServiceAlerts] = "http://api.carrismetropolitana.pt/alerts.pb"
            }),
        new GtfsData("carris", "https://gateway.carris.pt/gateway/gtfs/api/v2.11/GTFS"),
        new GtfsData("fertagus", "https://www.fertagus.pt/GTFSTMLzip/Fertagus_GTFS.zip"),
        new GtfsData("metro_lisboa", "https://www.metrolisboa.pt/google_transit/googleTransit.zip"),
        new GtfsData("mts", "https://mts.pt/imt/MTS-20240129.zip"),
        new GtfsData("giro", "https://drive.google.com/uc?export=download&id=1tkKV40lQlFcLiJhq8SwZQdxNVjFclRCA"),
        new GtfsData("tuf", "https://drive.google.com/uc?export=download&id=1xNHjM7yl-SS1jCkGrvIBBlOhUkFfNBjY"),
        new GtfsData("sobe_desce", "https://drive.google.com/uc?export=download&id=1C2KBOVsbm__ymWDgU1bXGbP0Gv3D2yo5"),
        new GtfsData("vamus", "https://drive.google.com/uc?export=download&id=1CM8O4ndsfSJhka42SxFUZ9eB-wE10NqX"),
        new GtfsData("a_onda", "https://drive.google.com/uc?export=download&id=1aPfsxHqopxxcjV8HlRzImzxh_a6zRxGp"),
        new GtfsData("apanha_me", "https://drive.google.com/uc?export=download&id=1w92h129CWNSoImBRZQOWT6KRPzSFwJ42"),
        new GtfsData("circuito_olhao", "https://drive.google.com/uc?export=download&id=1X5uXAYJ5ItxMhrnDZSz3v9-U3kgL6QbJ"),
        new GtfsData("hf_urbano", "http://www.horariosdofunchal.pt/google_transit.zip"),
        new GtfsData("tuvr", "http://www.urbanosvilareal.pt/fotos/gca/2019-01-10_gtfs_tuvr_v4_11987813585c3dbf6a271b6.zip"),
        new GtfsData("vai_vem", "https://drive.google.com/uc?export=download&id=1Ii02y25MCOilAaQixsZNs1rcgdvNoYCs"),
        new GtfsData("tub", "https://www.tub.pt/developer/gtfs/feed/tub.zip"),
        new GtfsData("tcp", "https://www.tcbarreiro.pt/front/files/sample_gtfs/GTFS-TCB_24.zip"),
        new GtfsData("rmtejo_ii", "https://drive.google.com/uc?export=download&id=1v9eGjezOzbaBQi-Aa5c15tZXcSiiDwqA"),
        new GtfsData("rdl_rodoviaria_lis", "https://drive.google.com/uc?export=download&id=1NqcOk8IrlV73TBonJAxpzIUTjaAjF27M")
    };

    public const string RootPath = "../Assets";

    public static string ExtractPath => Path.Combine(RootPath, "GtfsData", "Normal");
    public static string ExtractPathRealtime => Path.Combine(RootPath, "GtfsData", "Realtime");
    public static string TempDownloadFolder => Path.Combine(RootPath, "TempGtfs");

    public const string Name = "Transit GTFS";
    public const string Version = "1.0.0";

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