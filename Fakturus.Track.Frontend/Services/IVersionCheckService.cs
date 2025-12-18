namespace Fakturus.Track.Frontend.Services;

public interface IVersionCheckService
{
    Task CheckVersionAsync();
    Task StartPeriodicCheckAsync();
    void StopPeriodicCheck();
}



