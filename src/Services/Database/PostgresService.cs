using System.Diagnostics;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Tranzor.Context;
using Tranzor.Interfaces.Database;
using Tranzor.Utils;

namespace Tranzor.Services.Database;

public class PostgresService : IPostgresService
{
    private readonly GtfsDbContext _gtfsDbContext;
    private readonly ILogger<PostgresService> _logger;

    public PostgresService(GtfsDbContext gtfsDbContext, ILogger<PostgresService> logger)
    {
        _gtfsDbContext = gtfsDbContext;
        _logger = logger;
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
            LogPostgresConfiguration();
            
            _logger.LogInformation("[PostgreSQL] Checking database connection...");
            
            bool canConnect = await _gtfsDbContext.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                _logger.LogError("[PostgreSQL] Unable to connect to database");
                throw new InvalidOperationException("PostgreSQL connection failed");
            }

            _logger.LogInformation("[PostgreSQL] Connection established successfully");

            var pendingMigrations = await _gtfsDbContext.Database.GetPendingMigrationsAsync();
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

            var appliedMigrations = await _gtfsDbContext.Database.GetAppliedMigrationsAsync();
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
            var agenciesCount = await _gtfsDbContext.Agencies.CountAsync();
            var routesCount = await _gtfsDbContext.Routes.CountAsync();
            var stopsCount = await _gtfsDbContext.Stops.CountAsync();
            var tripsCount = await _gtfsDbContext.Trips.CountAsync();

            _logger.LogInformation("[PostgreSQL] Database statistics - Agencies: {Agencies}, Routes: {Routes}, Stops: {Stops}, Trips: {Trips}",
                agenciesCount, routesCount, stopsCount, tripsCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PostgreSQL] Unable to verify table statistics (tables may not exist yet)");
        }
    }

    /// <summary>
    /// Bulk insert entities into the database in batches
    /// </summary>
    /// <typeparam name="TEntity">Type of entity to import</typeparam>
    /// <param name="entities">List of entities to import</param>
    /// <param name="filePath">File path for logging purposes</param>
    /// <param name="totalIgnored">Total number of lines ignored</param>
    /// <param name="batchSize">Batch size (optional)</param>
    /// <returns>Total number of records imported</returns>
    public async Task<int> BulkInsertEntitiesAsync<TEntity>(
        List<TEntity> entities,
        string filePath,
        int totalIgnored = 0,
        int? batchSize = null
    ) where TEntity : class
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("[PostgreSQL] Starting bulk insert process for {FilePath}", filePath);

            int batch = batchSize ?? Constant.SqlBatchSizeImport;
            int totalImported = 0;

            for (int i = 0; i < entities.Count; i += batch)
            {
                List<TEntity> batchEntities = entities
                    .Skip(i)
                    .Take(batch)
                    .ToList();
                
                await _gtfsDbContext.BulkInsertAsync(batchEntities);
                totalImported += batchEntities.Count;
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "[PostgreSQL] Inserted {TotalImported} records from {FilePath} in database with {TotalIgnored} line(s) ignored. ({Duration})",
                totalImported,
                filePath,
                totalIgnored,
                TimeFormatUtil.FormatDurationFromMilliseconds((long)stopwatch.Elapsed.TotalMilliseconds)
            );

            return totalImported;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostgreSQL] Error importing data from {FilePath}", filePath);
            return 0;
        }
    }
}