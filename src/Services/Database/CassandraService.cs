using Cassandra;
using Tranzor.Interfaces.Database;

namespace Tranzor.Services.Database;

public class CassandraService : ICassandraService, IDisposable
{
    private readonly ILogger<CassandraService> _logger;
    private readonly ICluster? _cluster;
    private Cassandra.ISession? _session;
    private bool _disposed = false;

    public CassandraService(ILogger<CassandraService> logger)
    {
        _logger = logger;

        string? contactPoints = Environment.GetEnvironmentVariable("CASSANDRA_CONTACT_POINTS");
        string? keyspace = Environment.GetEnvironmentVariable("CASSANDRA_KEYSPACE");
        string? username = Environment.GetEnvironmentVariable("CASSANDRA_USERNAME");
        string? password = Environment.GetEnvironmentVariable("CASSANDRA_PASSWORD");

        if (string.IsNullOrWhiteSpace(contactPoints))
        {
            _logger.LogError("[Cassandra] CASSANDRA_CONTACT_POINTS environment variable is not set");
            throw new InvalidOperationException("CASSANDRA_CONTACT_POINTS is required");
        }

        if (string.IsNullOrWhiteSpace(keyspace))
        {
            _logger.LogError("[Cassandra] CASSANDRA_KEYSPACE environment variable is not set");
            throw new InvalidOperationException("CASSANDRA_KEYSPACE is required");
        }

        string[] hosts = contactPoints.Split(',');

        Builder builder = Cluster.Builder()
            .AddContactPoints(hosts)
            .WithDefaultKeyspace(keyspace);

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            builder = builder.WithCredentials(username, password);
        }

        _cluster = builder.Build();

        _logger.LogInformation("[Cassandra] Cluster configured with contact points: {ContactPoints}", contactPoints);
    }

    public Cassandra.ISession GetSession()
    {
        if (_session == null)
        {
            throw new InvalidOperationException("Session not initialized. Call InitializeAsync first.");
        }

        return _session;
    }

    public async Task<Cassandra.ISession> GetSessionAsync()
    {
        if (_session == null)
        {
            await InitializeAsync();
        }

        return _session!;
    }

    public async Task InitializeAsync()
    {
        if (_session != null)
        {
            return;
        }

        if (_cluster == null)
        {
            throw new InvalidOperationException("Cluster is not initialized");
        }

        _session = await _cluster.ConnectAsync();

        string? keyspace = Environment.GetEnvironmentVariable("CASSANDRA_KEYSPACE");

        await CreateKeyspaceIfNotExistsAsync(keyspace!);
        await CreateStopTimesTableIfNotExistsAsync();

        _logger.LogInformation("[Cassandra] Session initialized successfully");
    }

    private async Task CreateKeyspaceIfNotExistsAsync(string keyspace)
    {
        string createKeyspaceQuery = $@"
            CREATE KEYSPACE IF NOT EXISTS {keyspace}
            WITH replication = {{'class': 'SimpleStrategy', 'replication_factor': 1}}";

        await _session!.ExecuteAsync(new SimpleStatement(createKeyspaceQuery));
        _logger.LogInformation("[Cassandra] Keyspace {Keyspace} created or already exists", keyspace);
    }

    private async Task CreateStopTimesTableIfNotExistsAsync()
    {
        string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS stop_times (
                trip_id text,
                stop_sequence int,
                id text,
                arrival_time text,
                departure_time text,
                stop_id text,
                stop_headsign text,
                pickup_type int,
                drop_off_type int,
                shape_dist_traveled double,
                timepoint int,
                PRIMARY KEY ((trip_id), stop_sequence)
            ) WITH CLUSTERING ORDER BY (stop_sequence ASC)";

        await _session!.ExecuteAsync(new SimpleStatement(createTableQuery));

        string createStopIdIndexQuery = @"
            CREATE INDEX IF NOT EXISTS stop_times_stop_id_idx 
            ON stop_times (stop_id)";

        await _session.ExecuteAsync(new SimpleStatement(createStopIdIndexQuery));

        _logger.LogInformation("[Cassandra] Table stop_times and indexes created or already exist");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session?.Dispose();
        _cluster?.Dispose();
        _disposed = true;

        _logger.LogInformation("[Cassandra] Connection disposed");
    }
}
