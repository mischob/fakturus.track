namespace Fakturus.Track.WebApp.Models;

public class LegalVersionsResponse
{
    public List<LegalDocumentInfo> Documents { get; set; } = new();
}

public class LegalDocumentInfo
{
    public string Type { get; set; } = "";
    public int Version { get; set; }
    public string EffectiveDate { get; set; } = "";
    public string Url { get; set; } = "";
    public bool RequiresConsent { get; set; }
    public bool RequiresReConsent { get; set; }
}

public class ConsentSubmitRequest
{
    public List<ConsentItem> Consents { get; set; } = new();
}

public class ConsentItem
{
    public string DocumentType { get; set; } = "";
    public int DocumentVersion { get; set; }
    public bool ConsentGiven { get; set; }
}

public class ConsentStatusResponse
{
    public List<ConsentRecord> Consents { get; set; } = new();
    public bool AllRequiredConsentsGiven { get; set; }
    public List<string> PendingConsents { get; set; } = new();
}

public class ConsentRecord
{
    public string DocumentType { get; set; } = "";
    public int DocumentVersion { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime ConsentTimestamp { get; set; }
}
