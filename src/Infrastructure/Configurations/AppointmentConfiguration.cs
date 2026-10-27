using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Infrastructure.Persistence;

namespace UnifiedServiceScheduler.Infrastructure.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(StronglyTypedIdConverters.AppointmentIdConverter)
            .ValueGeneratedNever();

        // Foreign reference keys (mapped by ID without cross-aggregate navigation properties)
        builder.Property(a => a.DealershipId)
            .HasConversion(StronglyTypedIdConverters.DealershipIdConverter)
            .IsRequired();

        builder.Property(a => a.CustomerId)
            .HasConversion(StronglyTypedIdConverters.CustomerIdConverter)
            .IsRequired();

        builder.Property(a => a.TechnicianId)
            .HasConversion(StronglyTypedIdConverters.TechnicianIdConverter)
            .IsRequired();

        builder.Property(a => a.ServiceBayId)
            .HasConversion(StronglyTypedIdConverters.ServiceBayIdConverter)
            .IsRequired();

        builder.Property(a => a.ServiceTypeId)
            .HasConversion(StronglyTypedIdConverters.ServiceTypeIdConverter)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        builder.Property(a => a.ActualStart);
        builder.Property(a => a.ActualEnd);

        // Owned Value Object: TimeSlot
        builder.OwnsOne(a => a.Slot, slot =>
        {
            slot.Property(s => s.Start)
                .HasColumnName("StartTime")
                .IsRequired();

            slot.Property(s => s.End)
                .HasColumnName("EndTime")
                .IsRequired();

            slot.Ignore(s => s.Duration);
        });

        // Owned Value Object: VehicleSnapshot
        builder.OwnsOne(a => a.VehicleSnapshot, vs =>
        {
            vs.Property(v => v.Id)
                .HasConversion(StronglyTypedIdConverters.VehicleIdConverter)
                .HasColumnName("VehicleId")
                .IsRequired();

            vs.Property(v => v.Make)
                .HasColumnName("VehicleMake")
                .HasMaxLength(50)
                .IsRequired();

            vs.Property(v => v.Model)
                .HasColumnName("VehicleModel")
                .HasMaxLength(50)
                .IsRequired();

            vs.Property(v => v.Year)
                .HasColumnName("VehicleYear")
                .IsRequired();

            vs.Property(v => v.Vin)
                .HasColumnName("VehicleVin")
                .HasMaxLength(17)
                .IsFixedLength()
                .IsRequired();

            vs.Property(v => v.LicensePlate)
                .HasColumnName("VehicleLicensePlate")
                .HasMaxLength(20)
                .IsRequired();
        });

        // Indexes for high-frequency queries
        builder.HasIndex(a => new { a.DealershipId, a.Status });
        builder.HasIndex(a => a.TechnicianId);
        builder.HasIndex(a => a.ServiceBayId);

        // Ignore domain events from persistence
        builder.Ignore(a => a.DomainEvents);
    }
}
