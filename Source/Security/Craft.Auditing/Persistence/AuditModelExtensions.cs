using Craft.Auditing.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Craft.Auditing;

public static class AuditModelExtensions
{
    extension(ModelBuilder modelBuilder)
    {
        public ModelBuilder ConfigureAuditing(string tableName = "AuditTrails", string? schema = null)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            var entity = modelBuilder.Entity<AuditTrail>();
            entity.ToTable(tableName, schema);
            entity.HasKey(a => a.Id);
            var utc = new ValueConverter<DateTime, DateTime>(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
            entity.Property(a => a.OccurredAtUtc).HasConversion(utc);
            entity.Property(a => a.ArchiveAfterUtc).HasConversion(utc);
            entity.Property(a => a.Id).ValueGeneratedNever();
            entity.Property(a => a.EntityName).HasMaxLength(512).IsRequired();
            entity.Property(a => a.EntityKey).HasMaxLength(64).IsRequired();
            entity.Property(a => a.UserId).HasMaxLength(256);
            entity.Property(a => a.TenantId).HasMaxLength(256);
            entity.Property(a => a.CorrelationId).HasMaxLength(256);
            entity.Property(a => a.KeyValues).IsRequired();
            entity.Property(a => a.ChangedProperties).IsRequired();
            entity.HasIndex(a => new { a.EntityName, a.TenantId, a.EntityKey });
            entity.HasIndex(a => new { a.OccurredAtUtc, a.Id });
            entity.HasIndex(a => new { a.IsArchived, a.ArchiveAfterUtc });
            return modelBuilder;
        }
    }
}