namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NICE.Platform.Collaboration.Core.Entities;
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(x => x.Id);
        // TODO: configure column types, indexes, and relationships
    }
}
