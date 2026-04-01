namespace Fakturus.Track.Backend.DTOs;

// GET /api/legal/versions
public record LegalVersionsResponse(List<LegalDocumentInfo> Documents);

public record LegalDocumentInfo(
    string Type,
    int Version,
    string EffectiveDate,
    string Url,
    bool RequiresConsent,
    bool RequiresReConsent
);

// POST /api/legal/consent
public record SubmitConsentRequest(List<ConsentItem> Consents);

public record ConsentItem(
    string DocumentType,
    int DocumentVersion,
    bool ConsentGiven
);

// GET /api/legal/consent
public record UserConsentStatusResponse(
    List<UserConsentRecord> Consents,
    bool AllRequiredConsentsGiven,
    List<string> PendingConsents
);

public record UserConsentRecord(
    string DocumentType,
    int DocumentVersion,
    bool ConsentGiven,
    DateTime ConsentTimestamp
);
