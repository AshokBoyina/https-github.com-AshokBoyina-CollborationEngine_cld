namespace NICE.Platform.Collaboration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NICE.Platform.Collaboration.Core.Entities;
using System.Reflection;
public class CollaborationDbContext(DbContextOptions<CollaborationDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationRegistration> Applications   { get; set; }
    public DbSet<UserProfile>             Users          { get; set; }
    public DbSet<ApplicationUser>         ApplicationUsers { get; set; }
    public DbSet<AgentSession>            AgentSessions  { get; set; }
    public DbSet<Collaboration>           Collaborations { get; set; }
    public DbSet<ChatMessage>             ChatMessages   { get; set; }
    public DbSet<CollaborationRecording>  Recordings     { get; set; }
    public DbSet<TransferRequest>         TransferRequests { get; set; }
    public DbSet<BotMessage>              BotMessages    { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
        => builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
