namespace TransitGtfsApi.Utils;

public class MathUtil
{
    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = Math.PI / 180 * (lat2 - lat1);
        double dLon = Math.PI / 180 * (lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(Math.PI / 180 * lat1) * Math.Cos(Math.PI / 180 * lat2) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public static double DegreesToRadians(double deg) => deg * (Math.PI / 180);
}
