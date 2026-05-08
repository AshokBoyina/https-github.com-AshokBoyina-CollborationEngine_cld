namespace NICE.Platform.Collaboration.Infrastructure.Auth.Validators;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NICE.Platform.Collaboration.Application.Auth;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using NICE.Platform.Collaboration.Infrastructure.Auth.Settings;

/// <summary>
/// Validates a token by making an HTTP POST to the READI validate endpoint.
///
/// When <see cref="AuthValidationSettings.UseMock"/> is <c>true</c> the real
/// HTTP call is skipped and the configured mock response is returned immediately.
///
/// Expected READI response shape:
/// <code>
/// {
///   "success":   true,
///   "userId":    "...",
///   "email":     "...",
///   "firstName": "...",
///   "lastName":  "..."
/// }
/// </code>
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
        _http     = http;
        _settings = settings.Value;
        _logger   = logger;
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
            var requestBody = new { token = authToken };
            using var response = await _http.PostAsJsonAsync(url, requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("READI validate endpoint returned HTTP {Status}.", (int)response.StatusCode);
                return AuthValidatorResult.Fail($"READI validation rejected (HTTP {(int)response.StatusCode}).");
            }

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

    // ── Inner response DTO ────────────────────────────────────────────────────

    private sealed class ReadiValidateResponse
    {
        public bool    Success   { get; set; }
        public string? UserId    { get; set; }
        public string? Email     { get; set; }
        public string? FirstName { get; set; }
        public string? LastName  { get; set; }
        public string? Error     { get; set; }
    }
}
