namespace NICE.Platform.Collaboration.Contracts.Requests;

/// <summary>
/// All authentication parameters arrive as HTTP request headers — there is no body.
///
/// Required headers:
///
///   X-Api-Key    — the registered application's secret API key.
///                  Validated (hashed) against the application store.
///
///   X-Access-Key — the Application Name (e.g. "SurveyPortal", "CustomerSupport").
///                  The engine looks up the application by this name and derives
///                  the AuthProvider (READI | NICE | ANON) from its stored configuration.
///                  The calling client never selects the auth provider directly.
///
///   AuthToken    — the token to be validated by the provider configured for the application.
///
///   UserType     — role of the connecting user:
///                  External | Internal | Agent | Supervisor | StandAlone
/// </summary>
public static class AuthHeaders
{
    /// <summary>The application's secret API key.</summary>
    public const string ApiKey    = "X-Api-Key";

    /// <summary>The Application Name — used to look up the application and its AuthProvider.</summary>
    public const string AccessKey = "X-Access-Key";

    /// <summary>The raw token forwarded to the configured auth provider for validation.</summary>
    public const string AuthToken = "AuthToken";

    /// <summary>The connecting user's role: External | Internal | Agent | Supervisor | StandAlone.</summary>
    public const string UserType  = "UserType";
}
