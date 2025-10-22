using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tranzor.Utils;

namespace Tranzor.Context;

public class GtfsDbContextFactory : IDesignTimeDbContextFactory<GtfsDbContext>
{
    public GtfsDbContext CreateDbContext(string[] args)
    {
        string? path = FileUtil.ResolvePath(".env");

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            Env.Load(path);
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
