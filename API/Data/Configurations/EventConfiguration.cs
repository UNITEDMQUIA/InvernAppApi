using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvernApp.Api.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.DeviceId).HasMaxLength(50);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(e => e.Severity)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(e => e.Message).IsRequired().HasMaxLength(500);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_Events_CreatedAt");

        builder.HasIndex(e => new { e.DeviceId, e.CreatedAt })
            .HasDatabaseName("IX_Events_DeviceId_CreatedAt");

        // DeviceId es opcional: hay eventos del sistema sin dispositivo.
        builder.HasOne(e => e.Device)
            .WithMany(d => d.Events)
            .HasForeignKey(e => e.DeviceId)
            .HasPrincipalKey(d => d.DeviceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Trazabilidad: evento ← comando.
        builder.HasOne(e => e.Command)
            .WithMany()
            .HasForeignKey(e => e.CommandId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}