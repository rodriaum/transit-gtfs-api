using Tranzor.Models.Config;

namespace Tranzor.Context;

public static class GtfsDataContext
{
    public static bool Finish { get; set; } = false;
    public static ConfigData Config { get; set; }
    public static List<GtfsData> GtfsDataList { get; set; } = new();
}