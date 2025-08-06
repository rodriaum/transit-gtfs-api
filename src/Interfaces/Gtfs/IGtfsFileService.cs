namespace Tranzor.Interfaces.Gtfs;

public interface IGtfsFileService
{
    Task<List<string>> EnsureGtfsFilesExistAsync();
}