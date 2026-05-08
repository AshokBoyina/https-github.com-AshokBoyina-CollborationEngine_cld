namespace NICE.Platform.Collaboration.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Application.Auth;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using NICE.Platform.Collaboration.Contracts.Requests;
using NICE.Platform.Collaboration.Contracts.Responses;
using NICE.Platform.Collaboration.Core.Enums;

/// <summary>
/// Authentication entry point for the Collaboration Engine.
///
/// Every client calls POST api/v1/collaboration/auth/validate before opening
/// a SignalR connection. All parameters arrive as HTTP headers - there is no body.
///
/// Required headers:
///   X-Api-Key    - the registered application's secret API key
///   X-Access-Key - the Application Name (e.g. "SurveyPortal", "CustomerSupport")
///   AuthToken    - the raw token for the provider configured against that application
///   UserType     - External | Internal | Agent | Supervisor | StandAlone
///
/// Flow:
///   1. Validate X-Api-Key (non-empty; full DB hash check in Phase 2)
///   2. Look up application config by X-Access-Key (Application Name) from JSON / SQL
///   3. Derive AuthProvider (READI | NICE | ANON) from the application config
///   4. Validate AuthToken via the selected provider
///   5. Issue an engine session JWT
///   6. Return the session token + UserType-filtered ApplicationConfig
/// </summary>
[ApiController]
[Route("api/v1/collaboration/[controller]")]
[AllowAnonymous]
public class AuthController(
    IApplicationConfigProvider appConfigProvider,
    IAuthValidatorFactory       validatorFactory,
    ITokenService               tokenService) : ControllerBase
{
    /// <summary>
    /// Validates the external token and returns an engine session token + application config.
    /// </summary>
    /// <response code="200">Validation succeeded.</response>
    /// <response code="400">A required header is missing or the application name is unknown.</response>
    /// <response code="401">Token validation was rejected by the configured provider.</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(AuthValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthValidationResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthValidationResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Validate(CancellationToken ct)
    {
        // 1. Read required headers
        if (!TryGetHeader(AuthHeaders.ApiKey, out var apiKey))
            return BadRequest(Fail($"Missing required header '{AuthHeaders.ApiKey}'."));

        if (!TryGetHeader(AuthHeaders.AccessKey, out var applicationName))
            return BadRequest(Fail($"Missing required header '{AuthHeaders.AccessKey}' (Application Name)."));

        if (!TryGetHeader(AuthHeaders.AuthToken, out var authToken))
            return BadRequest(Fail($"Missing required header '{AuthHeaders.AuthToken}'."));

        if (!TryGetHeader(AuthHeaders.UserType, out var userTypeRaw))
            return BadRequest(Fail($"Missing required header '{AuthHeaders.UserType}'."));

        // 2. Parse UserType
        if (!Enum.TryParse<UserType>(userTypeRaw, ignoreCase: true, out var userType))
        {
            return BadRequest(Fail(
                $"Invalid '{AuthHeaders.UserType}' value '{userTypeRaw}'. " +
                $"Accepted: {string.Join(", ", Enum.GetNames<UserType>())}."));
        }

        // 3. Look up application config by name (JSON mock -> SQL in Phase 2)
        var appConfig = await appConfigProvider.GetByNameAsync(applicationName!, ct);
        if (appConfig is null)
        {
            return BadRequest(Fail(
                $"Application '{applicationName}' is not registered. " +
                $"Check the '{AuthHeaders.AccessKey}' header value."));
        }

        // 4. Derive AuthProvider from the application's stored config
        if (!Enum.TryParse<AuthProvider>(appConfig.AuthProvider, ignoreCase: true, out var provider))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, Fail(
                $"Application '{applicationName}' has an invalid AuthProvider " +
                $"'{appConfig.AuthProvider}' in its configuration."));
        }

        // 5. Validate the token via the selected provider
        var validator = validatorFactory.GetValidator(provider);
        var result    = await validator.ValidateAsync(authToken!, ct);

        if (!result.IsValid)
            return Unauthorized(Fail(result.Error ?? "Token validation failed."));

        // 6. Issue internal session token
        var sessionId  = Guid.NewGuid();
        var isExternal = userType == UserType.External;

        var sessionToken = tokenService.GenerateToken(
            userId:        Guid.TryParse(result.UserId, out var uid) ? uid : Guid.NewGuid(),
            role:          userType.ToString(),
            applicationId: Guid.Empty,
            sessionId:     sessionId,
            isExternal:    isExternal,
            firstName:     result.FirstName,
            lastName:      result.LastName,
            email:         result.Email,
            authProvider:  provider.ToString());

        // 7. Build the UserType-specific config slice
        var configDto = BuildConfigDto(appConfig, userType);

        // 8. Return response
        return Ok(new AuthValidationResponse
        {
            Success      = true,
            SessionToken = sessionToken,
            User = new AuthenticatedUserDto
            {
                UserId          = result.UserId,
                FirstName       = result.FirstName,
                LastName        = result.LastName,
                Email           = result.Email,
                SurveyId        = result.SurveyId,
                AuthProvider    = provider.ToString(),
                UserType        = userType.ToString(),
                ApplicationName = appConfig.Name,
                SessionId       = sessionId
            },
            ApplicationConfig = configDto
        });
    }

    // Config mapping - only the relevant UserType section is populated
    private static ApplicationConfigDto BuildConfigDto(ApplicationConfig cfg, UserType userType)
    {
        var dto = new ApplicationConfigDto { ApplicationName = cfg.Name };

        switch (userType)
        {
            case UserType.External when cfg.External is not null:
                dto.ExternalConfig = new ExternalUserConfigDto
                {
                    ChatMode            = cfg.External.ChatMode,
                    CanShareScreen      = cfg.External.CanShareScreen,
                    NeedScreenRecording = cfg.External.NeedScreenRecording
                };
                break;

            case UserType.Internal when cfg.Internal is not null:
                dto.InternalConfig = new InternalUserConfigDto
                {
                    ChatMode            = cfg.Internal.ChatMode,
                    CanShareScreen      = cfg.Internal.CanShareScreen,
                    NeedScreenRecording = cfg.Internal.NeedScreenRecording
                };
                break;

            case UserType.Agent when cfg.Agent is not null:
                dto.AgentConfig = new AgentConfigDto
                {
                    CanHandOffToOtherAgent = cfg.Agent.CanHandOffToOtherAgent,
                    MaxParallelChats       = cfg.Agent.MaxParallelChats
                };
                break;

            case UserType.Supervisor when cfg.Supervisor is not null:
                dto.SupervisorConfig = new SupervisorConfigDto
                {
                    MaxParallelChats       = cfg.Supervisor.MaxParallelChats,
                    CanHandOffToOtherAgent = cfg.Supervisor.CanHandOffToOtherAgent
                };
                break;

            case UserType.StandAlone when cfg.StandAlone is not null:
                dto.StandaloneConfig = new StandaloneConfigDto
                {
                    AutoRecordScreen       = cfg.StandAlone.AutoRecordScreen,
                    SupervisorCanWatchLive = cfg.StandAlone.SupervisorCanWatchLive
                };
                break;
        }

        return dto;
    }

    // Helpers
    private bool TryGetHeader(string name, out string? value)
    {
        if (Request.Headers.TryGetValue(name, out var sv) && !string.IsNullOrWhiteSpace(sv))
        {
            value = sv.ToString();
            return true;
        }
        value = null;
        return false;
    }

    private static AuthValidationResponse Fail(string error) =>
        new() { Success = false, Error = error };
}
