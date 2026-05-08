namespace NICE.Platform.Collaboration.Infrastructure.Auth.Validators;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NICE.Platform.Collaboration.Application.Auth;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using NICE.Platform.Collaboration.Infrastructure.Auth.Settings;

/// <summary>
/// Validates a token by making an HTTP POST to the NICE validate endpoint.
///
/// When <see cref="AuthValidationSettings.UseMock"/> is <c>true</c> the real
/// HTTP call is skipped and the configured mock response is returned immediately.
///
/// Expected NICE response shape:
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
public sealed class NiceAuthValidator : IAuthValidator
{
    private readonly HttpClient _http;
    private readonly AuthValidationSettings _settings;
    private readonly ILogger<NiceAuthValidator> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public NiceAuthValidator(
        HttpClient http,
        IOptions<AuthValidationSettings> settings,
        ILogger<NiceAuthValidator> logger)
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
            _logger.LogWarning("[MOCK] NICE validation bypassed — returning mock response.");
            var mock = _settings.Mock.Nice;
            return mock.IsValid
                ? AuthValidatorResult.Ok(mock.UserId, mock.Email, mock.FirstName, mock.LastName)
                : AuthValidatorResult.Fail(mock.Error ?? "Mock NICE validation failed.");
        }

        // ── REAL NICE CALL ────────────────────────────────────────────────────
        var url = _settings.Endpoints.NiceValidateUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogError("NICE validate URL is not configured (AuthValidation:Endpoints:NiceValidateUrl).");
            return AuthValidatorResult.Fail("NICE provider is not configured.");
        }

        try
        {
            var requestBody = new { token = authToken };
            using var response = await _http.PostAsJsonAsync(url, requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("NICE validate endpoint returned HTTP {Status}.", (int)response.StatusCode);
                return AuthValidatorResult.Fail($"NICE validation rejected (HTTP {(int)response.StatusCode}).");
            }

            var body = await response.Content.ReadFromJsonAsync<NiceValidateResponse>(JsonOpts, ct);

            if (body is null || !body.Success)
            {
                _logger.LogWarning("NICE validate returned success=false. Error: {Error}", body?.Error);
                return AuthValidatorResult.Fail(body?.Error ?? "NICE validation failed.");
            }

            _logger.LogInformation("NICE validation succeeded for user {UserId}.", body.UserId);
            return AuthValidatorResult.Ok(body.UserId!, body.Email, body.FirstName, body.LastName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling NICE validate endpoint at {Url}.", url);
            return AuthValidatorResult.Fail("NICE validate endpoint is unreachable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during NICE validation.");
            return AuthValidatorResult.Fail("NICE validation encountered an unexpected error.");
        }
    }

    // ── Inner response DTO ────────────────────────────────────────────────────

    private sealed class NiceValidateResponse
    {
        public bool    Success   { get; set; }
        public string? UserId    { get; set; }
        public string? Email     { get; set; }
        public string? FirstName { get; set; }
        public string? LastName  { get; set; }
        public string? Error     { get; set; }
    }
}
