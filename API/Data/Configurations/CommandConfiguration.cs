using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvernApp.Api.Data.Configurations;

public class CommandConfiguration : IEntityTypeConfiguration<Command>
{
    public void Configure(EntityTypeBuilder<Command> builder)
    {
        builder.ToTable("Commands", t =>
        {
            t.HasCheckConstraint("CK_Commands_Expiration", "[ExpiresAt] > [CreatedAt]");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.DeviceId).IsRequired().HasMaxLength(50);

        builder.Property(c => c.Type)
            .IsRequired()
            .HasMaxLength(30)
            .HasConversion<string>();

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(c => c.Reason).HasMaxLength(200);
        builder.Property(c => c.AckMessage).HasMaxLength(200);

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.ExpiresAt).IsRequired();

        // Consulta frecuente del ESP32: comandos pendientes de su dispositivo.
        builder.HasIndex(c => new { c.DeviceId, c.Status, c.CreatedAt })
            .HasDatabaseName("IX_Commands_DeviceId_Status_CreatedAt");

        builder.HasOne(c => c.Device)
            .WithMany(d => d.Commands)
            .HasForeignKey(c => c.DeviceId)
            .HasPrincipalKey(d => d.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}