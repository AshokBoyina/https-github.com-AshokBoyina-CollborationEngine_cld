namespace NICE.Platform.Collaboration.Contracts.Requests;
public class StartCollaborationRequest
{
    public Guid UserId { get; set; }
    public Guid AgentId { get; set; }
    public Guid ApplicationId { get; set; }
}
