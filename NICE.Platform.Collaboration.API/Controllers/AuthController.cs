namespace NICE.Platform.Collaboration.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using NICE.Platform.Collaboration.Contracts.Requests;
using NICE.Platform.Collaboration.Contracts.Responses;

/// <summary>
/// Pre-flight authentication endpoint.
/// The client calls POST /api/auth/validate with:
///   - the external provider JWT in the "X-Api-Key" header, and
///   - user identity + application name in the JSON request body.
/// The JWT is validated cryptographically (signature, issuer, audience, expiry).
/// User details are accepted from the body as the token does not embed them.
/// On success the response echoes back the resolved identity and application context,
/// which the client uses before opening a SignalR connection.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]   // No internal JWT required — this is the entry point
public class AuthController(IExternalAuthService externalAuth) : ControllerBase
{
    private const string ApiKeyHeader = "X-Api-Key";

    /// <summary>
    /// Validates the external provider JWT supplied in the <c>X-Api-Key</c> header
    /// against the provider identified by <c>ApplicationName</c> in the request body.
    /// </summary>
    /// <response code="200">Token is cryptographically valid — returns user identity.</response>
    /// <response code="400">Request body is missing required fields.</response>
    /// <response code="401">Token is missing, malformed, expired, or issued by an unknown provider.</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(AuthValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthValidationResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Validate(
        [FromBody] AuthValidateRequest request,
        CancellationToken ct)
    {
        // ── 1. Read JWT from header ───────────────────────────────────────────
        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var rawToken) ||
            string.IsNullOrWhiteSpace(rawToken))
        {
            return Unauthorized(new AuthValidationResponse
            {
                Success = false,
                Error   = $"Missing or empty '{ApiKeyHeader}' header."
            });
        }

        // ── 2. Validate the token against the provider named in the body ──────
        var result = await externalAuth.ValidateAsync(rawToken!, request.ApplicationName, ct);

        if (!result.IsValid)
        {
            return Unauthorized(new AuthValidationResponse
            {
                Success = false,
                Error   = result.Error
            });
        }

        // ── 3. Return success — user identity comes from the request body ─────
        return Ok(new AuthValidationResponse
        {
            Success = true,
            User    = new AuthenticatedUserDto
            {
                FirstName       = request.FirstName,
                LastName        = request.LastName,
                Email           = request.Email,
                ApplicationName = result.ApplicationName!,
                ApplicationId   = result.ApplicationId
            }
        });
    }
}
