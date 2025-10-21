using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tranzor.Context;

public class GtfsDbContextFactory : IDesignTimeDbContextFactory<GtfsDbContext>
{
    public GtfsDbContext CreateDbContext(string[] args)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string envPath = Path.Combine(baseDirectory, ".env");

        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }
        else
        {
            string rootPath = Path.Combine(baseDirectory, "..", "..", "..", "..", ".env");
            if (File.Exists(rootPath))
            {
                DotNetEnv.Env.Load(rootPath);
            }
        }

        DbContextOptionsBuilder<GtfsDbContext> optionsBuilder = new DbContextOptionsBuilder<GtfsDbContext>();

        string? connection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
        string? dbName = Environment.GetEnvironmentVariable("POSTGRES_DATABASE_NAME");

        if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(dbName))
        {
            throw new InvalidOperationException("POSTGRES_CONNECTION and POSTGRES_DATABASE_NAME must be set in the environment variables to spin up migrations.");
        }

        string fullConnection = $"{connection};Database={dbName}";
        optionsBuilder.UseNpgsql(fullConnection, o => o.UseNetTopologySuite())
                      .UseSnakeCaseNamingConvention();

        return new GtfsDbContext(optionsBuilder.Options);
    }
}
