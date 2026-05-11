namespace NICE.Platform.Collaboration.Infrastructure.Auth.Validators;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NICE.Platform.Collaboration.Application.Auth;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using NICE.Platform.Collaboration.Infrastructure.Auth.Settings;

/// <summary>
/// Validates a token by calling the READI refresh/validate endpoint and extracting user claims
/// from the response header `X-Readi-Token`. Falls back to JSON body parsing if header is not present.
/// Robustly handles `sub` encoded as a JSON array (e.g. ["A","B"]) inside the JWT payload.
/// </summary>
public sealed class ReadiAuthValidator : IAuthValidator
{
    private readonly HttpClient _http;
    private readonly AuthValidationSettings _settings;
    private readonly ILogger<ReadiAuthValidator> _logger;

    // JSON serializer options — case-insensitive so READI can use any casing.
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ReadiAuthValidator(
        HttpClient http,
        IOptions<AuthValidationSettings> settings,
        ILogger<ReadiAuthValidator> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AuthValidatorResult> ValidateAsync(string authToken, CancellationToken ct = default)
    {
        // ── MOCK MODE ─────────────────────────────────────────────────────────
        if (_settings.UseMock)
        {
            _logger.LogWarning("[MOCK] READI validation bypassed — returning mock response.");
            var mock = _settings.Mock.Readi;
            return mock.IsValid
                ? AuthValidatorResult.Ok(mock.UserId, mock.Email, mock.FirstName, mock.LastName)
                : AuthValidatorResult.Fail(mock.Error ?? "Mock READI validation failed.");
        }

        // ── REAL READI CALL ───────────────────────────────────────────────────
        var url = _settings.Endpoints.ReadiValidateUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogError("READI validate URL is not configured (AuthValidation:Endpoints:ReadiValidateUrl).");
            return AuthValidatorResult.Fail("READI provider is not configured.");
        }

        try
        {
            // Use GET because endpoint returned Allow: GET (no content)
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrWhiteSpace(authToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                // Mirror Postman/curl behaviour — some servers accept token as cookie instead of header.
                request.Headers.TryAddWithoutValidation("Cookie", $"AuthToken={authToken}");
            }

            using var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("READI validate endpoint returned HTTP {Status}.", (int)response.StatusCode);
                return AuthValidatorResult.Fail($"READI validation rejected (HTTP {(int)response.StatusCode}).");
            }

            // 1) Prefer JWT returned in response header "X-Readi-Token"
            if (response.Headers.TryGetValues("X-Readi-Token", out var headerValues))
            {
                var raw = headerValues.FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(raw))
                {
                    // Header might include "Bearer <token>" or just the token — strip if needed.
                    if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        raw = raw["Bearer ".Length..].Trim();

                    try
                    {
                        // Robust extraction: prefer decoded payload parsing (handles array-encoded sub)
                        var userId = GetClaimFromJwtPayload(raw, "sub")
                                     ?? TryReadJwtAndGetClaim(raw, jwt => jwt.Subject);

                        var givenName = GetClaimFromJwtPayload(raw, "given_name")
                                        ?? TryReadJwtAndGetClaim(raw, jwt => jwt.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value);

                        var familyName = GetClaimFromJwtPayload(raw, "family_name")
                                         ?? TryReadJwtAndGetClaim(raw, jwt => jwt.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value);

                        var email = GetClaimFromJwtPayload(raw, "email")
                                    ?? TryReadJwtAndGetClaim(raw, jwt => jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value);

                        _logger.LogInformation("READI validation succeeded for user {UserId}.", userId);
                        return AuthValidatorResult.Ok(userId ?? string.Empty, email, givenName, familyName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract claims from X-Readi-Token header. Falling back to body parsing.");
                        // fall through to body parsing
                    }
                }
            }

            // 2) Fallback — try to parse JSON body if header not present or parsing failed.
            var body = await response.Content.ReadFromJsonAsync<ReadiValidateResponse>(JsonOpts, ct);

            if (body is null || !body.Success)
            {
                _logger.LogWarning("READI validate returned success=false. Error: {Error}", body?.Error);
                return AuthValidatorResult.Fail(body?.Error ?? "READI validation failed.");
            }

            _logger.LogInformation("READI validation succeeded for user {UserId}.", body.UserId);
            return AuthValidatorResult.Ok(body.UserId!, body.Email, body.FirstName, body.LastName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling READI validate endpoint at {Url}.", url);
            return AuthValidatorResult.Fail("READI validate endpoint is unreachable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during READI validation.");
            return AuthValidatorResult.Fail("READI validation encountered an unexpected error.");
        }
    }

    // Decode Base64Url JWT payload and get a claim value.
    private static string? GetClaimFromJwtPayload(string jwt, string claimName)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];

            // Base64Url -> Base64
            var padded = payload.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
                case 0: break;
                default: padded += string.Empty; break;
            }

            var bytes = Convert.FromBase64String(padded);
            var json = Encoding.UTF8.GetString(bytes);

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(claimName, out var prop)) return null;

            return prop.ValueKind switch
            {
                JsonValueKind.Array when prop.GetArrayLength() > 0 =>
                    prop.EnumerateArray().Select(e => e.GetString()).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)),

                JsonValueKind.String => prop.GetString(),

                _ => prop.ToString()
            };
        }
        catch
        {
            return null;
        }
    }

    // Safely try to read using JwtSecurityTokenHandler and return a claim via selector.
    private static string? TryReadJwtAndGetClaim(string jwt, Func<JwtSecurityToken, string?> selector)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parsed = handler.ReadJwtToken(jwt);
            return selector(parsed);
        }
        catch
        {
            return null;
        }
    }

    // ── Inner response DTO ────────────────────────────────────────────────────
    private sealed class ReadiValidateResponse
    {
        public bool Success { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Error { get; set; }
    }
}
