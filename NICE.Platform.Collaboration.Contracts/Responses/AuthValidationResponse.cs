namespace NICE.Platform.Collaboration.Contracts.Responses;

/// <summary>
/// Returned by POST /api/auth/validate.
/// On success, <see cref="User"/> is populated.
/// On failure, <see cref="Error"/> explains why validation was rejected.
/// </summary>
public class AuthValidationResponse
{
    public bool   Success { get; set; }
    public string? Error  { get; set; }

    /// <summary>Populated only when <see cref="Success"/> is true.</summary>
    public AuthenticatedUserDto? User { get; set; }
}

/// <summary>Identity extracted from the validated external JWT.</summary>
public class AuthenticatedUserDto
{
    public string? FirstName       { get; set; }
    public string? LastName        { get; set; }
    public string? Email           { get; set; }
    public string  ApplicationName { get; set; } = default!;
    public Guid    ApplicationId   { get; set; }
}
