namespace Tranzor.Interfaces.Database;

public interface IPostgresService
{
    Task InitializeAsync();
    
    /// <summary>
    /// Bulk insert entities into the database in batches
    /// </summary>
    /// <typeparam name="TEntity">Type of entity to import</typeparam>
    /// <param name="entities">List of entities to import</param>
    /// <param name="filePath">File path for logging purposes</param>
    /// <param name="totalIgnored">Total number of lines ignored</param>
    /// <param name="batchSize">Batch size (optional)</param>
    /// <returns>Total number of records imported</returns>
    Task<int> BulkInsertEntitiesAsync<TEntity>(
        List<TEntity> entities,
        string filePath,
        int totalIgnored = 0,
        int? batchSize = null
    ) where TEntity : class;
}
