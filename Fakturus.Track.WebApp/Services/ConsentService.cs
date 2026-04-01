namespace Fakturus.Track.WebApp.Services;

public class ConsentService
{
    private readonly ApiClient _apiClient;
    private bool? _hasConsents;

    public bool HasRequiredConsents => _hasConsents ?? false;
    public bool IsLoaded => _hasConsents.HasValue;

    public event Action? OnConsentChanged;

    public ConsentService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task LoadConsentStatusAsync()
    {
        try
        {
            _hasConsents = await _apiClient.HasAcceptedConsentsAsync();
        }
        catch
        {
            _hasConsents = false;
        }
    }

    public async Task AcceptConsentsAsync()
    {
        await _apiClient.AcceptConsentsAsync();
        _hasConsents = true;
        OnConsentChanged?.Invoke();
    }
}
