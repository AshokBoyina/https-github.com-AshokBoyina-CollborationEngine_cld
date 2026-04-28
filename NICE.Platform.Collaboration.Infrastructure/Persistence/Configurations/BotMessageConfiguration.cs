namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NICE.Platform.Collaboration.Core.Entities;
public class BotMessageConfiguration : IEntityTypeConfiguration<BotMessage>
{
    public void Configure(EntityTypeBuilder<BotMessage> builder)
    {
        builder.HasKey(x => x.Id);
        // TODO: configure column types, indexes, and relationships
    }
}
