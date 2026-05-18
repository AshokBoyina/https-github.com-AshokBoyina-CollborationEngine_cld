namespace NICE.Platform.Collaboration.Infrastructure.Auth.Validators;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NICE.Platform.Collaboration.Application.Auth;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using NICE.Platform.Collaboration.Infrastructure.Auth.Settings;

/// <summary>
/// Anonymous validator — no external HTTP call is made.
/// The <c>AuthToken</c> is a JWT whose payload must contain:
///   <list type="bullet">
///     <item><c>surveyId</c>  — required</item>
///     <item><c>firstName</c> — required</item>
///     <item><c>lastName</c>  — required</item>
///   </list>
/// The signature is NOT cryptographically verified here (the token is self-contained
/// and the claims themselves are what the engine trusts).  If your deployment
/// requires signature verification, supply the key in appsettings and enable it.
///
/// When <see cref="AuthValidationSettings.UseMock"/> is <c>true</c> the token is
/// not decoded — the configured mock response is returned immediately.
/// </summary>
public sealed class AnonymousAuthValidator : IAuthValidator
{
    private const string ClaimSurveyId  = "surveyId";
    private const string ClaimFirstName = "firstName";
    private const string ClaimLastName  = "lastName";
    private const string ClaimEmail     = "email";
    private const string ClaimSub       = "sub";

    private readonly AuthValidationSettings _settings;
    private readonly ILogger<AnonymousAuthValidator> _logger;
    private readonly JwtSecurityTokenHandler _handler = new() { MapInboundClaims = false };

    public AnonymousAuthValidator(
        IOptions<AuthValidationSettings> settings,
        ILogger<AnonymousAuthValidator> logger)
    {
        _settings = settings.Value;
        _logger   = logger;
    }

    public Task<AuthValidatorResult> ValidateAsync(string authToken, CancellationToken ct = default)
    {
        // ── MOCK MODE ─────────────────────────────────────────────────────────
        if (_settings.UseMock)
        {
            _logger.LogWarning("[MOCK] ANON validation bypassed — token-aware mock.");
            var mock = _settings.Mock.Anon;
            if (!mock.IsValid)
                return Task.FromResult(AuthValidatorResult.Fail(mock.Error ?? "Mock ANON validation failed."));

            // Use the token itself as the ExternalUserId so demo users are distinguishable.
            // Parse "alice-smith" → firstName=Alice, lastName=Smith.
            var mockUserId   = string.IsNullOrWhiteSpace(authToken) ? mock.UserId : authToken;
            var (first, last) = ParseDemoName(authToken, mock.FirstName, mock.LastName);
            var mockSurveyId = mock.SurveyId ?? mockUserId;
            return Task.FromResult(AuthValidatorResult.Ok(mockUserId, mock.Email, first, last, mockSurveyId));
        }

        // ── REAL ANON VALIDATION — decode JWT claims internally ───────────────
        if (string.IsNullOrWhiteSpace(authToken))
            return Task.FromResult(AuthValidatorResult.Fail("AuthToken is missing or empty."));

        JwtSecurityToken jwt;
        try
        {
            jwt = _handler.ReadJwtToken(authToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ANON validator: failed to parse JWT.");
            return Task.FromResult(AuthValidatorResult.Fail("AuthToken is not a valid JWT."));
        }

        // Extract required claims
        var surveyId  = GetClaim(jwt, ClaimSurveyId);
        var firstName = GetClaim(jwt, ClaimFirstName);
        var lastName  = GetClaim(jwt, ClaimLastName);

        if (string.IsNullOrWhiteSpace(surveyId))
            return Task.FromResult(AuthValidatorResult.Fail("Anonymous JWT is missing the 'surveyId' claim."));

        if (string.IsNullOrWhiteSpace(firstName))
            return Task.FromResult(AuthValidatorResult.Fail("Anonymous JWT is missing the 'firstName' claim."));

        if (string.IsNullOrWhiteSpace(lastName))
            return Task.FromResult(AuthValidatorResult.Fail("Anonymous JWT is missing the 'lastName' claim."));

        // Optional claims
        var email  = GetClaim(jwt, ClaimEmail);
        var userId = GetClaim(jwt, ClaimSub) ?? surveyId;   // fall back to surveyId as userId

        _logger.LogInformation(
            "ANON validation succeeded for SurveyId={SurveyId}, Name={FirstName} {LastName}.",
            surveyId, firstName, lastName);

        return Task.FromResult(AuthValidatorResult.Ok(
            userId:    userId,
            firstName: firstName,
            lastName:  lastName,
            email:     email,
            surveyId:  surveyId));
    }

    private static string? GetClaim(System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt, string claimType)
        => jwt.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

    /// <summary>
    /// Converts a demo token slug to a (First, Last) name pair.
    /// "alice-smith" → ("Alice", "Smith"). Falls back to provided defaults.
    /// Called by all three validators in mock mode.
    /// </summary>
    public static (string First, string Last) ParseDemoName(
        string? token, string defaultFirst, string defaultLast)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (defaultFirst, defaultLast);

        var parts = token.Trim().Split('-', 2, StringSplitOptions.RemoveEmptyEntries);

        static string Cap(string s) =>
            s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

        return parts.Length >= 2
            ? (Cap(parts[0]), Cap(parts[1]))
            : (Cap(parts[0]), defaultLast);
    }
}
