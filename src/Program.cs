using Serilog;

namespace TransitGtfsApi;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            path: $"../logs/log-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            rollingInterval: RollingInterval.Infinite,
            retainedFileCountLimit: 10
        )
        .CreateLogger();


        try
        {
            Log.Information("Starting web application");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.ConfigureKestrel(options =>
                {
                    options.Limits.MaxConcurrentConnections = 10000;
                    options.Limits.MaxConcurrentUpgradedConnections = 10000;
                    options.Limits.MaxRequestBodySize = 10 * 1024; // 10 KB
                    options.Limits.MinRequestBodyDataRate = null;
                    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
                    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                });
            });
}