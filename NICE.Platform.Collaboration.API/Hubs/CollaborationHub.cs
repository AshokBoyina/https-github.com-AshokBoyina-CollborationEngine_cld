namespace NICE.Platform.Collaboration.API.Hubs;

using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.AcceptCollaboration;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.EndCollaboration;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.InviteSupervisor;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.StartCollaboration;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.SupervisorJoin;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.TransferCollaboration;
using NICE.Platform.Collaboration.Application.Features.Messages.Commands.SendMessage;
using NICE.Platform.Collaboration.Application.Features.Messages.Queries.GetMessages;
using NICE.Platform.Collaboration.Application.Features.Recordings.Commands.StartRecording;
using NICE.Platform.Collaboration.Application.Features.Recordings.Commands.StopRecording;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
using NICE.Platform.Collaboration.Contracts.Constants;
using NICE.Platform.Collaboration.Core.Entities;
using NICE.Platform.Collaboration.Infrastructure.Persistence;

/// <summary>
/// Central real-time hub for all collaboration activity.
/// Auth: Requires internal JWT from POST /api/v1/auth/validate.
/// For WebSocket connections pass the token via ?access_token=...
/// </summary>
[Authorize]
public sealed class CollaborationHub(
    ISender                   sender,
    CollaborationDbContext     db,
    IIceServerProvider        iceProvider,
    ILogger<CollaborationHub> logger) : Hub
{
    // ── Claim helpers ───────────────────────────────────────────────────────
    private Guid   CurrentUserId        => ParseGuid(Claim(ClaimTypes.NameIdentifier) ?? Claim("sub"));
    private Guid   CurrentApplicationId => ParseGuid(Claim("app"));
    private Guid   CurrentSessionId     => ParseGuid(Claim("sid"));
    private string CurrentUserType      => Claim(ClaimTypes.Role) ?? Claim("role") ?? "External";
    private string CurrentAuthProvider  => Claim("provider") ?? "UNKNOWN";
    // ASP.NET Core JWT middleware maps "given_name" → ClaimTypes.GivenName and
    // "family_name" → ClaimTypes.Surname, so we must try the mapped type first.
    private string CurrentFirstName     => Claim(ClaimTypes.GivenName)  ?? Claim("given_name")   ?? "";
    private string CurrentLastName      => Claim(ClaimTypes.Surname)    ?? Claim("family_name")   ?? "";
    private string CurrentDisplayName   => $"{CurrentFirstName} {CurrentLastName}".Trim();

    private string? Claim(string type) => Context.User?.FindFirstValue(type);

    private static Guid ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : Guid.Empty;

    // ── Lifecycle ───────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var connId = Context.ConnectionId;
        var userId = CurrentUserId;
        var appId  = CurrentApplicationId;
        var sessId = CurrentSessionId;
        var now    = DateTime.UtcNow;

        logger.LogInformation(
            "Hub connected: user={UserId} app={AppId} type={UserType} conn={ConnId}",
            userId, appId, CurrentUserType, connId);

        if (userId == Guid.Empty || appId == Guid.Empty)
        {
            logger.LogWarning("Rejecting hub connection — missing JWT claims. conn={ConnId}", connId);
            Context.Abort();
            return;
        }

        // ── CurrentSessions: upsert ────────────────────────────────────────
        // Remove any stale row for this session token (e.g. browser refresh
        // reconnects with the same JWT before the old disconnect is processed).
        var sessionId = sessId == Guid.Empty ? Guid.NewGuid() : sessId;
        var stale = await db.CurrentSessions.FindAsync(sessionId);
        if (stale is not null)
            db.CurrentSessions.Remove(stale);

        // ── Single-session enforcement ─────────────────────────────────────
        // Find all active sessions for this user on OTHER connections
        // (i.e. different browser/machine). Tell them to log out, then remove.
        var duplicateSessions = await db.CurrentSessions
            .Where(s => s.UserId == userId && s.SignalRConnectionId != connId)
            .ToListAsync();

        foreach (var dup in duplicateSessions)
        {
            try
            {
                await Clients.Client(dup.SignalRConnectionId!)
                    .SendAsync("ForceDisconnect", "You have been signed in from another device.");
            }
            catch { /* connection may already be gone */ }
            db.CurrentSessions.Remove(dup);
        }
        if (duplicateSessions.Count > 0)
            await db.SaveChangesAsync();

        var current = new CollaborationCurrentSession
        {
            Id                  = sessionId,
            ApplicationId       = appId,
            UserId              = userId,
            UserType            = CurrentUserType,
            AuthProvider        = CurrentAuthProvider,
            SignalRConnectionId = connId,
            ConnectedAt         = now,
            LastSeenAt          = now
        };
        await db.CurrentSessions.AddAsync(current);

        // ── UserSessions: always a fresh row per connection ────────────────
        // Using a new GUID (not the JWT session ID) means each browser session /
        // reconnect gets its own history row — no PK clash on refresh.
        var history = new CollaborationUserSession
        {
            Id            = Guid.NewGuid(),
            ApplicationId = appId,
            UserId        = userId,
            UserType      = CurrentUserType,
            AuthProvider  = CurrentAuthProvider,
            ConnectedAt   = now
        };
        await db.UserSessions.AddAsync(history);
        await db.SaveChangesAsync();

        // Add to application-wide group + role-specific groups
        await Groups.AddToGroupAsync(connId, SignalRGroups.Application(appId));
        if (CurrentUserType == "Agent")
            await Groups.AddToGroupAsync(connId, SignalRGroups.Agent(userId));
        if (CurrentUserType == "Supervisor")
            await Groups.AddToGroupAsync(connId, SignalRGroups.Supervisor(userId));
        if (CurrentUserType is "StandaloneMonitor")
            await Groups.AddToGroupAsync(connId, SignalRGroups.StandaloneMonitor(appId));

        // Push ICE config so client can initialise WebRTC immediately
        var iceConfig = await iceProvider.GetConfigAsync();
        await Clients.Caller.SendAsync("IceServersReady", iceConfig);

        await BroadcastOnlineUsersAsync(appId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connId = Context.ConnectionId;
        var now    = DateTime.UtcNow;

        logger.LogInformation(
            "Hub disconnected: conn={ConnId} reason={Reason}",
            connId, exception?.Message ?? "clean");

        var current = await db.CurrentSessions
            .FirstOrDefaultAsync(s => s.SignalRConnectionId == connId);

        if (current is not null)
        {
            // Track whether we need to end an active collaboration
            Guid? collabToEnd = null;

            // Stamp participant record if they were mid-collaboration
            if (current.CurrentCollaborationId.HasValue)
            {
                var participant = await db.Participants.FirstOrDefaultAsync(
                    p => p.CollaborationId == current.CurrentCollaborationId.Value
                      && p.UserId          == current.UserId
                      && p.LeftAt          == null);
                if (participant is not null)
                {
                    participant.LeftAt = now;
                    db.Participants.Update(participant);
                }

                // When an agent drops, end the whole collaboration so the customer
                // and supervisor are notified instead of hanging indefinitely.
                if (current.UserType == "Agent")
                {
                    var collab = await db.Collaborations.FindAsync(current.CurrentCollaborationId.Value);
                    if (collab is not null && collab.Status == "Active")
                    {
                        collab.Status    = "Ended";
                        collab.EndedAt   = now;
                        collab.EndReason = "Agent disconnected";
                        db.Collaborations.Update(collab);

                        // Stamp any other participants still open (customer, supervisor)
                        var openParts = await db.Participants
                            .Where(p => p.CollaborationId == collab.Id
                                     && p.LeftAt          == null
                                     && p.UserId          != current.UserId)
                            .ToListAsync();
                        foreach (var p in openParts)
                        {
                            p.LeftAt = now;
                            db.Participants.Update(p);
                        }

                        collabToEnd = collab.Id;
                        logger.LogInformation(
                            "Ending collaboration {CollabId} because agent {UserId} disconnected.",
                            collab.Id, current.UserId);
                    }
                }
            }

            db.CurrentSessions.Remove(current);

            // Stamp history row — look up by SignalRConnectionId because the
            // history row now uses its own Guid (not current.Id) to avoid PK clashes
            // when the same JWT session reconnects after a browser refresh.
            var history = await db.UserSessions
                .Where(s => s.UserId == current.UserId
                         && s.EndedAt == null
                         && s.ConnectedAt >= current.ConnectedAt.AddSeconds(-5))
                .OrderByDescending(s => s.ConnectedAt)
                .FirstOrDefaultAsync();
            if (history is not null)
            {
                history.EndedAt         = now;
                history.DurationSeconds = (int)(now - history.ConnectedAt).TotalSeconds;
                history.EndReason       = exception is null ? "Disconnected" : "Error";
                db.UserSessions.Update(history);
            }

            await db.SaveChangesAsync();

            await BroadcastOnlineUsersAsync(current.ApplicationId);

            // If an active collaboration was ended by the agent's disconnect,
            // broadcast CollaborationEnded to ALL relevant parties so every UI updates.
            if (collabToEnd.HasValue)
            {
                var endPayload = new
                {
                    id        = collabToEnd.Value.ToString(),
                    status    = "Ended",
                    endReason = "Agent disconnected"
                };

                // Direct participants (customer, joined supervisor)
                await Clients
                    .Group(SignalRGroups.Collaboration(collabToEnd.Value))
                    .SendAsync("CollaborationEnded", endPayload);

                // Supervisors monitoring silently (not yet joined)
                await Clients
                    .Group(SignalRGroups.SilentMonitor(collabToEnd.Value))
                    .SendAsync("CollaborationEnded", endPayload);

                // Whole application so supervisor dashboards remove the session
                await Clients
                    .Group(SignalRGroups.Application(current.ApplicationId))
                    .SendAsync("CollaborationEnded", endPayload);
            }
            else if (current.CurrentCollaborationId.HasValue)
            {
                // Non-agent (customer / supervisor) disconnected mid-session —
                // notify participants so the agent sees a "participant left" notice.
                await Clients
                    .Group(SignalRGroups.Collaboration(current.CurrentCollaborationId.Value))
                    .SendAsync("ParticipantDisconnected", new
                    {
                        UserId   = current.UserId,
                        UserType = current.UserType,
                        Timestamp = now
                    });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── Heartbeat ───────────────────────────────────────────────────────────

    /// <summary>Clients call every 30 s to refresh LastSeenAt (presence detection).</summary>
    public async Task Heartbeat()
    {
        var current = await db.CurrentSessions.FirstOrDefaultAsync(
            s => s.SignalRConnectionId == Context.ConnectionId);
        if (current is null) return;
        current.LastSeenAt = DateTime.UtcNow;
        db.CurrentSessions.Update(current);
        await db.SaveChangesAsync();
    }

    // ── Group management ────────────────────────────────────────────────────

    public async Task JoinCollaborationGroup(string collaborationId)
    {
        var collabGuid = Guid.Parse(collaborationId);
        await Groups.AddToGroupAsync(Context.ConnectionId,
            SignalRGroups.Collaboration(collabGuid));

        var current = await db.CurrentSessions.FirstOrDefaultAsync(
            s => s.SignalRConnectionId == Context.ConnectionId);
        if (current is not null)
        {
            current.CurrentCollaborationId = collabGuid;
            db.CurrentSessions.Update(current);
            await db.SaveChangesAsync();

            // Notify all internal users in the app so they can refresh their channel browser
            if (current.UserType == "Internal")
                await BroadcastInternalChannelsAsync(current.ApplicationId);
        }
    }

    public async Task LeaveCollaborationGroup(string collaborationId)
    {
        var collabGuid = Guid.Parse(collaborationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,
            SignalRGroups.Collaboration(collabGuid));

        var current = await db.CurrentSessions.FirstOrDefaultAsync(
            s => s.SignalRConnectionId == Context.ConnectionId);
        if (current is not null && current.CurrentCollaborationId == collabGuid)
        {
            current.CurrentCollaborationId = null;
            db.CurrentSessions.Update(current);
            await db.SaveChangesAsync();

            // Notify all internal users in the app so they can refresh their channel browser
            if (current.UserType == "Internal")
                await BroadcastInternalChannelsAsync(current.ApplicationId);
        }
    }

    // ── Internal channel browser helper ────────────────────────────────────────
    /// <summary>
    /// Builds the current list of active internal channels for the given application
    /// and broadcasts it to all connections in that application group.
    /// </summary>
    private async Task BroadcastInternalChannelsAsync(Guid appId)
    {
        var sessions = await db.CurrentSessions
            .AsNoTracking()
            .Where(s => s.ApplicationId         == appId
                     && s.UserType              == "Internal"
                     && s.CurrentCollaborationId.HasValue)
            .Join(db.Users,
                  s => s.UserId,
                  u => u.Id,
                  (s, u) => new
                  {
                      ChannelId   = s.CurrentCollaborationId!.Value,
                      DisplayName = (u.FirstName + " " + u.LastName).Trim()
                  })
            .ToListAsync();

        var channels = sessions
            .GroupBy(x => x.ChannelId)
            .Select(g => new
            {
                ChannelId    = g.Key.ToString(),
                Participants = g.Select(x => x.DisplayName).Distinct().ToList()
            })
            .OrderBy(c => c.ChannelId)
            .ToList();

        await Clients.Group(SignalRGroups.Application(appId))
            .SendAsync("InternalChannelsUpdated", channels);
    }

    /// <summary>Supervisor joins silently — added to monitor group only, no presence announced.</summary>
    public async Task JoinSilently(string collaborationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId,
               SignalRGroups.SilentMonitor(Guid.Parse(collaborationId)));

    // ── Collaboration lifecycle ──────────────────────────────────────────────

    /// <summary>External user requests a live collaboration (escalation from bot/standalone).</summary>
    public async Task RequestCollaboration(string? preferredAgentId)
    {
        Guid? preferredAgent = Guid.TryParse(preferredAgentId, out var ag) ? ag : null;
        var result  = await sender.Send(
            new StartCollaborationCommand(CurrentUserId, preferredAgent, CurrentApplicationId));

        // Add external user to the collab group immediately so they receive future events
        await Groups.AddToGroupAsync(Context.ConnectionId,
            SignalRGroups.Collaboration(result.Id));

        // Resolve display name — prefer JWT claims, fall back to DB user record
        var displayName = CurrentDisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            var dbUser = await db.Users.FirstOrDefaultAsync(
                u => u.Id == CurrentUserId);
            if (dbUser != null)
                displayName = $"{dbUser.FirstName} {dbUser.LastName}".Trim();
        }
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = $"User {CurrentUserId.ToString()[..4].ToUpper()}";

        // Notify caller with basic info
        await Clients.Caller.SendAsync("CollaborationCreated", new
        {
            CollaborationId = result.Id.ToString(),
            Status          = result.Status
        });

        // Notify all agents/supervisors in the application with enriched payload
        await Clients
            .Group(SignalRGroups.Application(CurrentApplicationId))
            .SendAsync("NewCollaborationRequest", new
            {
                CollaborationId = result.Id.ToString(),
                Status          = result.Status,
                CustomerName    = displayName,
                CustomerUserId  = CurrentUserId.ToString()
            });
    }

    /// <summary>Agent accepts an incoming collaboration (or re-joins a transferred one).</summary>
    public async Task AcceptCollaboration(string collaborationId)
    {
        var collabGuid = Guid.Parse(collaborationId);

        // For transferred sessions the collab is already "Active" and the command throws.
        // We still need to add the agent to the SignalR group and notify the customer,
        // so wrap the business-logic command in a try-catch.
        string resultStatus = "Active";
        try
        {
            var result = await sender.Send(new AcceptCollaborationCommand(collabGuid, CurrentUserId));
            resultStatus = result.Status;
        }
        catch (Exception ex)
        {
            // Expected for transferred sessions — log and continue.
            logger.LogWarning(
                "AcceptCollaboration command failed for {CollabId} (likely a transfer re-join): {Msg}",
                collabGuid, ex.Message);
        }

        // Always join SignalR groups — idempotent, safe to call multiple times.
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Collaboration(collabGuid));
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.SilentMonitor(collabGuid));

        // Resolve agent display name — JWT claims first, DB fallback, last resort UID prefix
        var agentDisplayName = CurrentDisplayName;
        if (string.IsNullOrWhiteSpace(agentDisplayName))
        {
            var dbAgent = await db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
            if (dbAgent != null)
                agentDisplayName = $"{dbAgent.FirstName} {dbAgent.LastName}".Trim();
        }
        if (string.IsNullOrWhiteSpace(agentDisplayName))
            agentDisplayName = $"Agent {CurrentUserId.ToString()[..4].ToUpper()}";

        var payload = new
        {
            CollaborationId = collabGuid.ToString(),
            Status          = resultStatus,
            AgentId         = CurrentUserId.ToString(),
            AgentName       = agentDisplayName
        };

        // Notify the collaboration group (customer sees agent name, transitions out of Searching)
        await Clients
            .Group(SignalRGroups.Collaboration(collabGuid))
            .SendAsync("CollaborationAccepted", payload);

        // Notify ALL agents in the application so they dismiss their incoming popup
        await Clients
            .Group(SignalRGroups.Application(CurrentApplicationId))
            .SendAsync("CollaborationRequestTaken", collabGuid.ToString());

        // Push message history to this agent so they see the full conversation.
        // Wrap with collaborationId so the client can route to the correct session
        // regardless of which session the agent is currently viewing.
        try
        {
            var history = (await sender.Send(new GetMessagesQuery(collabGuid))).ToList();
            if (history.Count > 0)
                await Clients.Caller.SendAsync("ChatHistory", new
                {
                    collaborationId = collabGuid.ToString(),
                    messages        = history
                });
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to push chat history for {CollabId}: {Msg}", collabGuid, ex.Message);
        }
    }

    /// <summary>Any participant ends the collaboration.</summary>
    public async Task EndCollaboration(string collaborationId, string? reason)
    {
        var collabGuid = Guid.Parse(collaborationId);
        var result     = await sender.Send(
            new EndCollaborationCommand(collabGuid, CurrentUserId, reason ?? "Completed"));

        // 1. Direct participants (customer, agent, supervisor who joined the group)
        await Clients
            .Group(SignalRGroups.Collaboration(collabGuid))
            .SendAsync("CollaborationEnded", result);

        // 2. Supervisors watching silently (read-only, not in the main collab group)
        await Clients
            .Group(SignalRGroups.SilentMonitor(collabGuid))
            .SendAsync("CollaborationEnded", result);

        // 3. ALL users in the application so every supervisor dashboard updates
        //    its active-session list even if they were not watching this session.
        //    Double-delivery is safe: the client handler is idempotent.
        await Clients
            .Group(SignalRGroups.Application(CurrentApplicationId))
            .SendAsync("CollaborationEnded", result);
    }

    /// <summary>Agent transfers to another agent.</summary>
    public async Task TransferCollaboration(
        string collaborationId, string toAgentId, string? reason)
    {
        var collabGuid  = Guid.Parse(collaborationId);
        var toAgentGuid = Guid.Parse(toAgentId);

        await sender.Send(
            new TransferCollaborationCommand(collabGuid, CurrentUserId, toAgentGuid, reason));

        // Fetch customer name for the notification payload
        var collab = await db.Collaborations
            .Include(c => c.ExternalUser)
            .FirstOrDefaultAsync(c => c.Id == collabGuid);

        var customerName = collab?.ExternalUser != null
            ? $"{collab.ExternalUser.FirstName} {collab.ExternalUser.LastName}".Trim()
            : "Customer";

        var transferPayload = new
        {
            CollaborationId = collabGuid.ToString(),
            CustomerName    = customerName,
            FromAgentId     = CurrentUserId.ToString(),
            FromAgentName   = CurrentDisplayName,
            ToAgentId       = toAgentGuid.ToString(),
            Reason          = reason ?? ""
        };

        // Notify the collab group (customer + current agent see it)
        await Clients
            .Group(SignalRGroups.Collaboration(collabGuid))
            .SendAsync("CollaborationTransferred", transferPayload);

        // Notify the new agent's personal group
        await Clients
            .Group(SignalRGroups.Agent(toAgentGuid))
            .SendAsync("TransferReceived", transferPayload);
    }

    /// <summary>Agent invites a supervisor to observe or assist.</summary>
    public async Task InviteSupervisor(string collaborationId, string supervisorId)
    {
        var collabGuid     = Guid.Parse(collaborationId);
        var supervisorGuid = Guid.Parse(supervisorId);
        await sender.Send(new InviteSupervisorCommand(collabGuid, CurrentUserId, supervisorGuid));

        // Fetch customer name for the invite notification
        var collab = await db.Collaborations
            .Include(c => c.ExternalUser)
            .FirstOrDefaultAsync(c => c.Id == collabGuid);

        var customerName = collab?.ExternalUser != null
            ? $"{collab.ExternalUser.FirstName} {collab.ExternalUser.LastName}".Trim()
            : "Customer";

        var invitePayload = new
        {
            CollaborationId = collabGuid.ToString(),
            SupervisorId    = supervisorGuid.ToString(),
            CustomerName    = customerName,
            RequestingAgentId   = CurrentUserId.ToString(),
            RequestingAgentName = CurrentDisplayName
        };

        // Notify the target supervisor's group
        await Clients
            .Group(SignalRGroups.Supervisor(supervisorGuid))
            .SendAsync("SupervisorInviteReceived", invitePayload);
    }


    /// <summary>Supervisor officially joins (after accepting invite).</summary>
    public async Task SupervisorJoin(string collaborationId)
    {
        var collabGuid = Guid.Parse(collaborationId);
        await sender.Send(new SupervisorJoinCommand(collabGuid, CurrentUserId));

        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Collaboration(collabGuid));
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.SilentMonitor(collabGuid));

        await Clients
            .Group(SignalRGroups.Collaboration(collabGuid))
            .SendAsync("SupervisorJoined", collabGuid.ToString());
    }

    // ── Messaging ───────────────────────────────────────────────────────────

    /// <summary>Sends a chat message to all participants in a collaboration.</summary>
    public async Task SendMessage(string collaborationId, string content)
    {
        var collabGuid = Guid.Parse(collaborationId);

        var response = await sender.Send(
            new SendMessageCommand(collabGuid, CurrentUserId, content, null, "Text"));

        // Broadcast to everyone in the collaboration group (customer + agent + supervisor)
        await Clients
            .Group(SignalRGroups.Collaboration(collabGuid))
            .SendAsync("MessageReceived", response);
    }

    /// <summary>Sends an agent-to-supervisor whisper (internal note invisible to the customer).</summary>
    public async Task SendWhisper(string collaborationId, string content)
    {
        var collabGuid = Guid.Parse(collaborationId);

        var response = await sender.Send(
            new SendMessageCommand(collabGuid, CurrentUserId, content, null, "Whisper"));

        // Whispers go only to the silent-monitor group (supervisors) — NOT the customer
        await Clients
            .Group(SignalRGroups.SilentMonitor(collabGuid))
            .SendAsync("WhisperMessage", response);
    }

    // ── WebRTC screen-share signaling ────────────────────────────────────────
    // Used by Standalone recorder AND by the Agent screen-share flow.

    /// <summary>Sender (Standalone / Agent) broadcasts an SDP offer to the collab group.</summary>
    public async Task ShareScreenOffer(string collaborationId, string sdp)
    {
        if (!Guid.TryParse(collaborationId, out var collabGuid)) return;

        // Send to everyone in the collab group EXCEPT the caller (they are the offerer).
        // IMPORTANT: send as a named object so the client can deserialise by property name
        // (collabId, sdp).  Positional args arrive as a JSON array and the client's
        // TryGetProperty("collaborationId") / TryGetProperty("sdp") calls would return empty.
        await Clients
            .GroupExcept(SignalRGroups.Collaboration(collabGuid), Context.ConnectionId)
            .SendAsync("Offer", new { collaborationId = collabGuid.ToString(), sdp });
    }

    /// <summary>Observer (StandaloneMonitor / Supervisor) returns SDP answer to the offerer.</summary>
    public async Task ShareScreenAnswer(string collaborationId, string sdp)
    {
        if (!Guid.TryParse(collaborationId, out var collabGuid)) return;

        // Route answer back to everyone except the answerer — the offerer receives it.
        // Named object required — same reason as ShareScreenOffer above.
        await Clients
            .GroupExcept(SignalRGroups.Collaboration(collabGuid), Context.ConnectionId)
            .SendAsync("Answer", new { collaborationId = collabGuid.ToString(), sdp });
    }

    /// <summary>Monitor requests a fresh offer from the Standalone user currently sharing.</summary>
    public async Task RequestScreenOffer(string collaborationId)
    {
        if (!Guid.TryParse(collaborationId, out var collabGuid)) return;

        await Clients
            .GroupExcept(SignalRGroups.Collaboration(collabGuid), Context.ConnectionId)
            .SendAsync("ScreenOfferRequested", collabGuid.ToString());
    }

    /// <summary>Standalone user (or Agent) signals that screen-share has stopped.</summary>
    public async Task StopScreenShare(string collaborationId)
    {
        if (!Guid.TryParse(collaborationId, out var collabGuid)) return;

        await Clients
            .Group(SignalRGroups.Collaboration(collabGuid))
            .SendAsync("ScreenShareStopped", collabGuid.ToString());
    }

    // ── Standalone session management ────────────────────────────────────────

    /// <summary>
    /// Called by a Standalone user immediately on login.
    /// Creates a collaboration record, adds the user to the collab group, stamps
    /// CurrentCollaborationId, then broadcasts the new session to all monitors.
    /// </summary>
    public async Task StartStandaloneSession()
    {
        var userId = CurrentUserId;
        var appId  = CurrentApplicationId;

        // Reuse StartCollaborationCommand — creates a row in Collaborations table
        var result     = await sender.Send(new StartCollaborationCommand(userId, null, appId));
        var collabGuid = result.Id;

        // Add standalone user to the collaboration group so they receive Offer/Answer events
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Collaboration(collabGuid));

        // Stamp CurrentCollaborationId so GetStandaloneSessions can find this session
        var session = await db.CurrentSessions.FirstOrDefaultAsync(
            s => s.SignalRConnectionId == Context.ConnectionId);
        if (session is not null)
        {
            session.CurrentCollaborationId = collabGuid;
            db.CurrentSessions.Update(session);
            await db.SaveChangesAsync();
        }

        // Resolve display name — JWT claims first, then DB, then UID prefix fallback
        var displayName = CurrentDisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (dbUser != null)
                displayName = $"{dbUser.FirstName} {dbUser.LastName}".Trim();
        }
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = $"Standalone {userId.ToString()[..4].ToUpper()}";

        var sessionInfo = new
        {
            collaborationId = collabGuid.ToString(),
            userName        = displayName,
            userId          = userId.ToString(),
            startedAt       = DateTime.UtcNow,
            isStreaming     = false
        };

        // Confirm collabId back to the recorder
        await Clients.Caller.SendAsync("StandaloneSessionStarted", sessionInfo);

        // Notify all StandaloneMonitor users so their session sidebar updates instantly
        await Clients
            .Group(SignalRGroups.StandaloneMonitor(appId))
            .SendAsync("StandaloneSessionStarted", sessionInfo);

        logger.LogInformation(
            "Standalone session started: collab={CollabId} user={UserId} name={Name}",
            collabGuid, userId, displayName);
    }

    /// <summary>
    /// StandaloneMonitor joins a specific session to watch the live stream.
    /// Adds the monitor to the collab group, then triggers a re-offer from the standalone user.
    /// </summary>
    public async Task JoinStandaloneSession(string collaborationId)
    {
        if (!Guid.TryParse(collaborationId, out var collabGuid)) return;

        // Join the collab group — required to receive Offer / Answer events
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Collaboration(collabGuid));

        // Ask the standalone user in this collab group to re-offer their stream
        await Clients
            .GroupExcept(SignalRGroups.Collaboration(collabGuid), Context.ConnectionId)
            .SendAsync("ScreenOfferRequested", collabGuid.ToString());

        logger.LogInformation(
            "StandaloneMonitor {UserId} joined collab {CollabId}",
            CurrentUserId, collabGuid);
    }

    /// <summary>
    /// Returns the list of currently active Standalone sessions in this application.
    /// Sent only to the requesting caller so each monitor can build its sidebar.
    /// </summary>
    public async Task GetStandaloneSessions()
    {
        var appId = CurrentApplicationId;

        // IMPORTANT: do NOT call .ToString() inside the LINQ projection that hits the DB.
        // EF Core translates Guid.ToString() → SQL CAST(uniqueidentifier AS varchar) which
        // returns UPPERCASE UUIDs ("62CF9619-…").  The hub's ShareScreenOffer uses C#
        // Guid.ToString() which is always lowercase ("62cf9619-…").  The Monitor stores the
        // ID from this response in _selectedCollabId and later compares it to the offer's
        // collabId with a case-sensitive string equals — they never match → offer is ignored.
        // Fix: materialise raw Guid columns first, then project with C# ToString() (lowercase).
        var rows = await db.CurrentSessions
            .AsNoTracking()
            .Where(s => s.ApplicationId == appId
                     && (s.UserType == "Standalone" || s.UserType == "StandAlone")
                     && s.CurrentCollaborationId != null)
            .Join(db.Users,
                  s => s.UserId,
                  u => u.Id,
                  (s, u) => new
                  {
                      CollabId    = s.CurrentCollaborationId,   // Guid? — no SQL ToString
                      UserName    = (u.FirstName + " " + u.LastName).Trim(),
                      UserId      = s.UserId,
                      ConnectedAt = s.ConnectedAt
                  })
            .ToListAsync();

        // C# Guid.ToString() always produces lowercase UUIDs — matches ShareScreenOffer output.
        var sessions = rows.Select(r => new
        {
            collaborationId = r.CollabId!.Value.ToString(),
            userName        = r.UserName,
            userId          = r.UserId.ToString(),
            startedAt       = r.ConnectedAt,
            isStreaming     = true
        });

        await Clients.Caller.SendAsync("StandaloneSessionsList", sessions);
    }

    // ── Online presence ──────────────────────────────────────────────────────

    private async Task BroadcastOnlineUsersAsync(Guid appId)
    {
        var online = await db.CurrentSessions
            .AsNoTracking()
            .Where(s => s.ApplicationId == appId)
            .Join(db.Users,
                  s => s.UserId,
                  u => u.Id,
                  (s, u) => new
                  {
                      UserId      = s.UserId.ToString(),
                      DisplayName = (u.FirstName + " " + u.LastName).Trim(),
                      UserType    = s.UserType,
                      ConnectedAt = s.ConnectedAt
                  })
            .ToListAsync();

        await Clients
            .Group(SignalRGroups.Application(appId))
            .SendAsync("OnlineUsersUpdated", online);
    }
}
