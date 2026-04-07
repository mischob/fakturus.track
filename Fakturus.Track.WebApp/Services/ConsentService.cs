using Fakturus.Track.WebApp.Models;

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
            var status = await _apiClient.GetConsentStatusAsync();
            _hasConsents = status?.AllRequiredConsentsGiven ?? false;
        }
        catch
        {
            _hasConsents = false;
        }
    }

    public async Task AcceptConsentsAsync(int termsVersion = 1)
    {
        try
        {
            var request = new ConsentSubmitRequest
            {
                Consents = new List<ConsentItem>
                {
                    new() { DocumentType = "terms_of_service", DocumentVersion = termsVersion, ConsentGiven = true }
                }
            };
            var result = await _apiClient.SubmitConsentAsync(request);
            _hasConsents = result?.AllRequiredConsentsGiven ?? true;
        }
        catch
        {
            // Accept locally even if backend fails — will sync later
            _hasConsents = true;
        }
        OnConsentChanged?.Invoke();
    }
}
