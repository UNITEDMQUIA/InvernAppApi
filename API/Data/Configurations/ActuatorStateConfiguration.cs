using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvernApp.Api.Data.Configurations;

public class ActuatorStateConfiguration : IEntityTypeConfiguration<ActuatorState>
{
    public void Configure(EntityTypeBuilder<ActuatorState> builder)
    {
        builder.ToTable("ActuatorStates");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.DeviceId).IsRequired().HasMaxLength(50);

        builder.Property(a => a.Actuator)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(a => a.ChangedAt).IsRequired();

        // Un solo estado por actuador y dispositivo.
        builder.HasIndex(a => new { a.DeviceId, a.Actuator })
            .IsUnique()
            .HasDatabaseName("UX_ActuatorStates_DeviceId_Actuator");

        builder.HasOne(a => a.Device)
            .WithMany(d => d.ActuatorStates)
            .HasForeignKey(a => a.DeviceId)
            .HasPrincipalKey(d => d.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}