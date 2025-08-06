namespace Tranzor.Interfaces.Gtfs;

public interface IGtfsDataService
{
    Task InitializeAsync();
    Task LoadDataFromFilesAsync();
}