namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NICE.Platform.Collaboration.Core.Entities;
public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(x => x.Id);
        // TODO: configure column types, indexes, and relationships
    }
}
