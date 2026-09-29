using DesafioArquiteturaSoftware.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DesafioArquiteturaSoftware.Infrastructure.Data.Configurations;

public sealed class IdempotencyRecordConfiguration
    : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(
        EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RequestHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ResourceId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Key)
            .IsUnique()
            .HasDatabaseName("ux_idempotency_records_key");
    }
}