namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NICE.Platform.Collaboration.Core.Entities;
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasKey(x => new { x.ApplicationId, x.UserId });
        // TODO: configure column types, indexes, and relationships
    }
}
