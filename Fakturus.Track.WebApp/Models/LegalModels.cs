namespace Fakturus.Track.WebApp.Models;

public record LegalVersionsResponse(
    List<LegalDocumentInfo> Documents
);

public record LegalDocumentInfo(
    string Type,
    int Version,
    string EffectiveDate,
    string Url,
    bool RequiresConsent,
    bool RequiresReConsent
);

public record ConsentSubmitRequest(
    List<ConsentItem> Consents
);

public record ConsentItem(
    string DocumentType,
    int DocumentVersion,
    bool ConsentGiven
);

public record ConsentStatusResponse(
    List<ConsentRecord> Consents,
    bool AllRequiredConsentsGiven,
    List<string> PendingConsents
);

public record ConsentRecord(
    string DocumentType,
    int DocumentVersion,
    bool ConsentGiven,
    DateTime ConsentTimestamp
);
