namespace NICE.Platform.Collaboration.Infrastructure.Auth.Settings;

/// <summary>
/// Bound from <c>appsettings.json → AuthValidation</c>.
/// Controls real vs. mock validation behaviour for all providers.
/// </summary>
public class AuthValidationSettings
{
    public const string SectionName = "AuthValidation";

    /// <summary>
    /// When <c>true</c> every validator returns the configured mock response
    /// instead of making a real HTTP call or decoding a live JWT.
    /// Safe to enable in development / testing — must be <c>false</c> in production.
    /// </summary>
    public bool UseMock { get; set; } = false;

    /// <summary>Per-provider endpoint URLs used when <see cref="UseMock"/> is false.</summary>
    public ProviderEndpoints Endpoints { get; set; } = new();

    /// <summary>Static mock responses used when <see cref="UseMock"/> is true.</summary>
    public MockResponses Mock { get; set; } = new();
}

public class ProviderEndpoints
{
    /// <summary>Full URL of the READI validate endpoint (e.g. https://readi.example.com/api/validate).</summary>
    public string ReadiValidateUrl { get; set; } = default!;

    /// <summary>Full URL of the NICE validate endpoint (e.g. https://nice.example.com/api/validate).</summary>
    public string NiceValidateUrl { get; set; } = default!;
}

public class MockResponses
{
    public MockValidatorResponse Readi { get; set; } = new();
    public MockValidatorResponse Nice  { get; set; } = new();
    public MockValidatorResponse Anon  { get; set; } = new();
}

/// <summary>
/// Shape of the mock response that each validator will return when mock mode is active.
/// </summary>
public class MockValidatorResponse
{
    public bool    IsValid   { get; set; } = true;
    public string  UserId    { get; set; } = "mock-user-id";
    public string  Email     { get; set; } = "mock@example.com";
    public string  FirstName { get; set; } = "Mock";
    public string  LastName  { get; set; } = "User";
    public string? SurveyId  { get; set; }              // ANON only
    public string? Error     { get; set; }
}
