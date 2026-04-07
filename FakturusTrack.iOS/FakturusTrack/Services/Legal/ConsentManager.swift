import Foundation
import Observation

@Observable @MainActor
final class ConsentManager {
    private(set) var hasRequiredConsents = false
    private(set) var isChecking = false

    private static let termsVersionKey = "consent_terms_version"
    private static let termsSyncedKey = "consent_terms_synced"
    private static let privacyAcknowledgedKey = "consent_privacy_acknowledged"

    private var apiClient: APIClient?

    func configure(apiClient: APIClient?) {
        self.apiClient = apiClient
    }

    /// Check if all required consents are given
    func checkConsent() {
        let termsVersion = UserDefaults.standard.integer(forKey: Self.termsVersionKey)
        let privacyAcknowledged = UserDefaults.standard.bool(forKey: Self.privacyAcknowledgedKey)
        hasRequiredConsents = termsVersion > 0 && privacyAcknowledged
    }

    /// Record consent after user agrees
    func recordConsent(termsVersion: Int) {
        UserDefaults.standard.set(termsVersion, forKey: Self.termsVersionKey)
        UserDefaults.standard.set(false, forKey: Self.termsSyncedKey)
        UserDefaults.standard.set(true, forKey: Self.privacyAcknowledgedKey)
        hasRequiredConsents = true

        // Sync to backend
        Task { await syncConsentToBackend(termsVersion: termsVersion) }
    }

    /// Try to sync pending consent to backend
    func syncPendingConsent() async {
        let synced = UserDefaults.standard.bool(forKey: Self.termsSyncedKey)
        if synced { return }
        let version = UserDefaults.standard.integer(forKey: Self.termsVersionKey)
        if version > 0 {
            await syncConsentToBackend(termsVersion: version)
        }
    }

    /// Check for version updates from backend
    func checkForVersionUpdates() async {
        isChecking = true
        defer { isChecking = false }

        guard let client = apiClient else { return }

        do {
            let response: LegalVersionsAPIResponse = try await client.get("/api/legal/versions")
            let currentTermsVersion = UserDefaults.standard.integer(forKey: Self.termsVersionKey)

            for doc in response.documents {
                if doc.type == "terms_of_service" && doc.requiresReConsent && doc.version > currentTermsVersion {
                    hasRequiredConsents = false
                    return
                }
            }
        } catch {
            print("[ConsentManager] Version check failed: \(error)")
        }
    }

    /// Clear all consent data (on logout or account deletion)
    func clearConsent() {
        UserDefaults.standard.removeObject(forKey: Self.termsVersionKey)
        UserDefaults.standard.removeObject(forKey: Self.termsSyncedKey)
        UserDefaults.standard.removeObject(forKey: Self.privacyAcknowledgedKey)
        hasRequiredConsents = false
    }

    // MARK: - Private

    private func syncConsentToBackend(termsVersion: Int) async {
        guard let client = apiClient else { return }

        do {
            let request = ConsentSubmitRequest(consents: [
                ConsentSubmitItem(documentType: "terms_of_service", documentVersion: termsVersion, consentGiven: true)
            ])
            let _: ConsentStatusResponse = try await client.post("/api/legal/consent", body: request)
            UserDefaults.standard.set(true, forKey: Self.termsSyncedKey)
            print("[ConsentManager] Consent synced to backend")
        } catch {
            print("[ConsentManager] Backend sync failed, will retry: \(error)")
        }
    }
}

// MARK: - API Models

private struct LegalVersionsAPIResponse: Decodable {
    let documents: [LegalDocumentAPIInfo]
}

private struct LegalDocumentAPIInfo: Decodable {
    let type: String
    let version: Int
    let effectiveDate: String
    let url: String
    let requiresConsent: Bool
    let requiresReConsent: Bool
}

private struct ConsentSubmitRequest: Encodable {
    let consents: [ConsentSubmitItem]
}

private struct ConsentSubmitItem: Encodable {
    let documentType: String
    let documentVersion: Int
    let consentGiven: Bool
}

private struct ConsentStatusResponse: Decodable {
    let consents: [ConsentRecord]
    let allRequiredConsentsGiven: Bool
    let pendingConsents: [String]
}

private struct ConsentRecord: Decodable {
    let documentType: String
    let documentVersion: Int
    let consentGiven: Bool
    let consentTimestamp: String
}
