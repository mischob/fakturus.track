import SwiftUI

struct ConsentView: View {
    let onConsented: () -> Void
    let onDeclined: () -> Void

    @State private var agreedToTerms = false
    @State private var openedPrivacy = false
    @State private var showDeclineAlert = false

    var canProceed: Bool {
        agreedToTerms && openedPrivacy
    }

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 24) {
                    // Header
                    VStack(spacing: 8) {
                        Image(systemName: "doc.text.fill")
                            .font(.system(size: 48))
                            .foregroundStyle(.blue)

                        Text(String(localized: "consent_title"))
                            .font(.title2.bold())
                            .multilineTextAlignment(.center)

                        Text(String(localized: "consent_subtitle"))
                            .font(.body)
                            .foregroundStyle(.secondary)
                            .multilineTextAlignment(.center)
                    }
                    .padding(.top, 32)

                    // Privacy Policy — Kenntnisnahme (not consent)
                    VStack(alignment: .leading, spacing: 8) {
                        HStack(spacing: 12) {
                            Image(systemName: openedPrivacy ? "checkmark.circle.fill" : "circle")
                                .foregroundStyle(openedPrivacy ? .green : .secondary)
                                .font(.title3)

                            VStack(alignment: .leading, spacing: 2) {
                                Text(String(localized: "consent_privacy_label"))
                                    .font(.subheadline.bold())
                                Text(String(localized: "consent_privacy_hint"))
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }
                        }

                        Button {
                            openedPrivacy = true
                            if let url = URL(string: "https://track.fakturus.com/privacy") {
                                UIApplication.shared.open(url)
                            }
                        } label: {
                            HStack {
                                Image(systemName: "arrow.up.right.square")
                                Text(String(localized: "consent_read_privacy"))
                            }
                            .font(.subheadline)
                        }
                        .padding(.leading, 36)
                    }
                    .padding()
                    .background(Color(.systemGray6))
                    .clipShape(RoundedRectangle(cornerRadius: 12))

                    // Terms — Active Checkbox (BGB §305)
                    VStack(alignment: .leading, spacing: 8) {
                        Button {
                            agreedToTerms.toggle()
                        } label: {
                            HStack(spacing: 12) {
                                Image(systemName: agreedToTerms ? "checkmark.square.fill" : "square")
                                    .foregroundStyle(agreedToTerms ? .blue : .secondary)
                                    .font(.title3)

                                VStack(alignment: .leading, spacing: 2) {
                                    Text(String(localized: "consent_terms_label"))
                                        .font(.subheadline.bold())
                                        .foregroundStyle(.primary)
                                    Text(String(localized: "consent_terms_hint"))
                                        .font(.caption)
                                        .foregroundStyle(.secondary)
                                }
                            }
                        }

                        Button {
                            if let url = URL(string: "https://track.fakturus.com/terms") {
                                UIApplication.shared.open(url)
                            }
                        } label: {
                            HStack {
                                Image(systemName: "arrow.up.right.square")
                                Text(String(localized: "consent_read_terms"))
                            }
                            .font(.subheadline)
                        }
                        .padding(.leading, 36)
                    }
                    .padding()
                    .background(Color(.systemGray6))
                    .clipShape(RoundedRectangle(cornerRadius: 12))

                    // Proceed button
                    Button {
                        onConsented()
                    } label: {
                        Text(String(localized: "consent_agree_button"))
                            .font(.headline)
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 14)
                    }
                    .buttonStyle(.borderedProminent)
                    .disabled(!canProceed)

                    // Decline
                    Button(String(localized: "consent_decline_button")) {
                        showDeclineAlert = true
                    }
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                }
                .padding()
            }
            .navigationTitle("")
            .navigationBarBackButtonHidden(true)
            .interactiveDismissDisabled()
            .alert(String(localized: "consent_decline_title"), isPresented: $showDeclineAlert) {
                Button(String(localized: "consent_decline_back"), role: .cancel) {}
                Button(String(localized: "consent_decline_logout"), role: .destructive) {
                    onDeclined()
                }
            } message: {
                Text(String(localized: "consent_decline_message"))
            }
        }
    }
}
