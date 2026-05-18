namespace NICE.Platform.Collaboration.ChatUI.Services;

using NICE.Platform.Collaboration.ChatUI.Models;

public interface IDemoApiService
{
    Task<bool>                  PingAsync(CancellationToken ct = default);
    Task<DemoStatusResult>      GetStatusAsync(CancellationToken ct = default);
    Task<(bool Ok, string Msg)> SeedAppsAsync(CancellationToken ct = default);
    Task<List<DemoUser>>        GetUsersAsync(Guid appId, CancellationToken ct = default);
    Task<DemoUser?>             CreateUserAsync(DemoCreateUserDto dto, CancellationToken ct = default);
    Task<bool>                  RemoveUserAsync(Guid userId, Guid appId, CancellationToken ct = default);
    Task<List<ActiveChannelInfo>>        GetActiveInternalChannelsAsync(Guid appId, CancellationToken ct = default);
    Task<List<CollaborationMessageDto>>  GetCollaborationMessagesAsync(Guid collaborationId, CancellationToken ct = default);
    /// <summary>Returns users currently connected to the hub (non-External only).</summary>
    Task<List<OnlineUserInfo>>           GetOnlineUsersAsync(Guid appId, CancellationToken ct = default);
}
