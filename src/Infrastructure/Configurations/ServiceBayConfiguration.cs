using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Infrastructure.Persistence;

namespace UnifiedServiceScheduler.Infrastructure.Configurations;

public sealed class ServiceBayConfiguration : IEntityTypeConfiguration<ServiceBay>
{
    public void Configure(EntityTypeBuilder<ServiceBay> builder)
    {
        builder.ToTable("ServiceBays");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(StronglyTypedIdConverters.ServiceBayIdConverter)
            .ValueGeneratedNever();

        builder.Property(b => b.DealershipId)
            .HasConversion(StronglyTypedIdConverters.DealershipIdConverter)
            .IsRequired();

        builder.Property(b => b.BayCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Capability)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.IsUnderMaintenance)
            .IsRequired();

        // Child Entities: Bookings (mapped via private backing field _bookings)
        builder.HasMany(b => b.Bookings)
            .WithOne()
            .HasForeignKey(bb => bb.BayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Bookings)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Optimistic Concurrency Token: Shadow Version property
        builder.Property<uint>("Version")
            .IsRowVersion()
            .IsConcurrencyToken();

        // Indexes
        builder.HasIndex(b => new { b.DealershipId, b.Capability, b.IsUnderMaintenance });
        builder.HasIndex(b => new { b.DealershipId, b.BayCode }).IsUnique();

        builder.Ignore(b => b.DomainEvents);
    }
}

public sealed class BayBookingConfiguration : IEntityTypeConfiguration<BayBooking>
{
    public void Configure(EntityTypeBuilder<BayBooking> builder)
    {
        builder.ToTable("BayBookings");

        // Composite natural key: (BayId, AppointmentId)
        builder.HasKey(bb => new { bb.BayId, bb.AppointmentId });

        builder.Property(bb => bb.BayId)
            .HasConversion(StronglyTypedIdConverters.ServiceBayIdConverter);

        builder.Property(bb => bb.AppointmentId)
            .HasConversion(StronglyTypedIdConverters.AppointmentIdConverter);

        builder.OwnsOne(bb => bb.Slot, slot =>
        {
            slot.Property(s => s.Start)
                .HasColumnName("StartTime")
                .IsRequired();

            slot.Property(s => s.End)
                .HasColumnName("EndTime")
                .IsRequired();

            slot.Ignore(s => s.Duration);

            // Index for overlap queries
            slot.HasIndex(s => new { s.Start, s.End });
        });
    }
}
