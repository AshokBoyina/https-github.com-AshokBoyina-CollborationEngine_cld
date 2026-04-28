namespace NICE.Platform.Collaboration.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NICE.Platform.Collaboration.Core.Entities;
public class TransferRequestConfiguration : IEntityTypeConfiguration<TransferRequest>
{
    public void Configure(EntityTypeBuilder<TransferRequest> builder)
    {
        builder.HasKey(x => x.Id);
        // TODO: configure column types, indexes, and relationships
    }
}
