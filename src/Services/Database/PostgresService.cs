using Microsoft.EntityFrameworkCore;
using Tranzor.Databases;
using Tranzor.Interfaces.Database;

namespace Tranzor.Services.Database;

public class PostgresService : IPostgresService
{
    private readonly GTFSContext _context;
    private readonly ILogger<PostgresService> _logger;

    public PostgresService(GTFSContext context, ILogger<PostgresService> logger)
    {
        _context = context;
        _logger = logger;
        
        LogPostgresConfiguration();
    }

    private void LogPostgresConfiguration()
    {
        string? connection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
        string? dbName = Environment.GetEnvironmentVariable("POSTGRES_DATABASE_NAME");
        
        string host = ExtractHost(connection);
        
        _logger.LogInformation("[PostgreSQL] Database configured with host: {Host}", host);
        _logger.LogInformation("[PostgreSQL] Target database: {Database}", dbName);
    }

    private string ExtractHost(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "not configured";

        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.Trim().StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Split('=')[1].Trim();
            }
        }
        
        return "configured";
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("[PostgreSQL] Checking database connection...");
            
            bool canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                _logger.LogError("[PostgreSQL] Unable to connect to database");
                throw new InvalidOperationException("PostgreSQL connection failed");
            }

            _logger.LogInformation("[PostgreSQL] Connection established successfully");

            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            var pendingCount = pendingMigrations.Count();

            if (pendingCount > 0)
            {
                _logger.LogInformation("[PostgreSQL] Found {Count} pending migration(s)", pendingCount);
                
                // await _context.Database.MigrateAsync();
                // _logger.LogInformation("[PostgreSQL] Database migrations applied successfully");
            }
            else
            {
                _logger.LogInformation("[PostgreSQL] Database schema is up to date");
            }

            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
            var appliedCount = appliedMigrations.Count();
            
            _logger.LogInformation("[PostgreSQL] Total migrations applied: {Count}", appliedCount);

            await VerifyTables();

            _logger.LogInformation("[PostgreSQL] Initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostgreSQL] Error initializing database");
            throw;
        }
    }

    private async Task VerifyTables()
    {
        try
        {
            var agenciesCount = await _context.Agencies.CountAsync();
            var routesCount = await _context.Routes.CountAsync();
            var stopsCount = await _context.Stops.CountAsync();
            var tripsCount = await _context.Trips.CountAsync();

            _logger.LogInformation("[PostgreSQL] Database statistics - Agencies: {Agencies}, Routes: {Routes}, Stops: {Stops}, Trips: {Trips}",
                agenciesCount, routesCount, stopsCount, tripsCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PostgreSQL] Unable to verify table statistics (tables may not exist yet)");
        }
    }
}