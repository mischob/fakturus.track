import Foundation

enum Configuration {
    // API
    static let apiBaseUrl: String = {
        #if DEBUG
        return "https://localhost:7001"
        #else
        return "https://api.track.fakturus.com"
        #endif
    }()

    // Azure AD B2C
    static let b2cTenant = "fakturus.onmicrosoft.com"
    static let b2cClientId = "3fb35bc6-8825-495e-b0a2-18e00352f968"
    static let b2cPolicy = "B2C_1_BetaSignInOnly"
    static let b2cRedirectUri = "msauth.com.fakturus.track://auth"
    static let b2cScopes = [
        "https://fakturus.onmicrosoft.com/74fd0ed2-8865-4bad-b002-7d867ad8791a/access"
    ]
    static let b2cAuthorityUrl =
        "https://fakturus.b2clogin.com/tfp/fakturus.onmicrosoft.com/B2C_1_BetaSignInOnly"
}
