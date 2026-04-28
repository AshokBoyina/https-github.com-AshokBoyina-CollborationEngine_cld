namespace NICE.Platform.Collaboration.Application.Interfaces.Auth;
public interface ITokenService
{
    string GenerateToken(Guid userId, string role, Guid applicationId, Guid sessionId, bool isExternal);
    (Guid userId, string role, Guid applicationId, Guid sessionId) ValidateToken(string token);
}
