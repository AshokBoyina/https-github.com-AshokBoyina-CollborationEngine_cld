namespace NICE.Platform.Collaboration.Contracts.Responses;
public class IceServerConfigResponse
{
    public List<string> Urls { get; set; } = [];
    public string? Username { get; set; }
    public string? Credential { get; set; }
}
