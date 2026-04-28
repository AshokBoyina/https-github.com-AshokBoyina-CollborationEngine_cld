namespace NICE.Platform.Collaboration.Contracts.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request body for POST /api/auth/validate.
/// The JWT token itself is supplied in the "X-Api-Key" request header.
/// User identity and the target application are provided here because
/// the external JWT does not embed those claims.
/// </summary>
public class AuthValidateRequest
{
    [Required]
    public string FirstName { get; set; } = default!;

    [Required]
    public string LastName { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    /// <summary>
    /// Must match a key in the "AuthProviders" appsettings section — e.g. "Readi" or "Nice".
    /// Used to select the correct OIDC provider for token validation.
    /// </summary>
    [Required]
    public string ApplicationName { get; set; } = default!;
}
