namespace NICE.Platform.Collaboration.Infrastructure.Auth;
using NICE.Platform.Collaboration.Application.Interfaces.Auth;
using Microsoft.Extensions.Configuration;
public class JwtTokenService(IConfiguration config) : ITokenService
{
    public string GenerateToken(Guid userId, string role, Guid applicationId, Guid sessionId, bool isExternal)
    {
        // TODO: build JWT with claims using config["Jwt:Key"], config["Jwt:Issuer"], etc.
        throw new NotImplementedException();
    }
    public (Guid userId, string role, Guid applicationId, Guid sessionId) ValidateToken(string token)
    {
        // TODO: validate and extract claims
        throw new NotImplementedException();
    }
}
