using Cassandra;

namespace Tranzor.Interfaces.Database;

public interface ICassandraService
{
    Cassandra.ISession GetSession();
    Task<Cassandra.ISession> GetSessionAsync();
    Task InitializeAsync();
}
