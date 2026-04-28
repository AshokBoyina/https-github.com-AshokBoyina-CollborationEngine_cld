namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NICE.Platform.Collaboration.Core.Entities;
public class ApplicationRegistrationConfiguration : IEntityTypeConfiguration<ApplicationRegistration>
{
    public void Configure(EntityTypeBuilder<ApplicationRegistration> builder)
    {
        builder.HasKey(x => x.Id);
        // TODO: configure column types, indexes, and relationships
    }
}
