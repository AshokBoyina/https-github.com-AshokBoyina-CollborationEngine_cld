namespace NICE.Platform.Collaboration.Contracts.Constants;
public static class SignalRGroups
{
    public static string Collaboration(Guid id) => $"collab-{id}";
    public static string Application(Guid id)   => $"app-{id}";
    public static string SilentMonitor(Guid id) => $"silent-{id}";
    public static string Recording(Guid id)     => $"recording-{id}";
    public static string Agent(Guid id)         => $"agent-{id}";
    public static string Supervisor(Guid id)    => $"supervisor-{id}";
}
