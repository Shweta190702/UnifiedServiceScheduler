using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Infrastructure.Persistence;

namespace UnifiedServiceScheduler.Infrastructure.Configurations;

public sealed class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.ToTable("Technicians");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(StronglyTypedIdConverters.TechnicianIdConverter)
            .ValueGeneratedNever();

        builder.Property(t => t.DealershipId)
            .HasConversion(StronglyTypedIdConverters.DealershipIdConverter)
            .IsRequired();

        builder.Property(t => t.EmployeeNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired();

        // Owned Value Object: ContactInfo
        builder.OwnsOne(t => t.Contact, c =>
        {
            c.Property(p => p.FirstName).HasColumnName("FirstName").HasMaxLength(100).IsRequired();
            c.Property(p => p.LastName).HasColumnName("LastName").HasMaxLength(100).IsRequired();
            c.Property(p => p.Email).HasColumnName("Email").HasMaxLength(255).IsRequired();
            c.Property(p => p.PhoneNumber).HasColumnName("PhoneNumber").HasMaxLength(50).IsRequired();
            c.Ignore(p => p.FullName);
        });

        // Owned Collection: Certifications
        builder.OwnsMany(t => t.Certifications, cb =>
        {
            cb.ToTable("TechnicianCertifications");
            cb.WithOwner().HasForeignKey("TechnicianId");
            cb.Property<Guid>("Id").ValueGeneratedOnAdd();
            cb.HasKey("Id");

            cb.Property(c => c.ServiceCategory).HasMaxLength(100).IsRequired();
            cb.Property(c => c.Level).HasConversion<string>().HasMaxLength(50).IsRequired();
            cb.Property(c => c.IssuedOn).IsRequired();
            cb.Property(c => c.ExpiresOn);

            cb.HasIndex("TechnicianId", "ServiceCategory");
        });

        // Child Entities: Bookings (mapped via private backing field _bookings)
        builder.HasMany(t => t.Bookings)
            .WithOne()
            .HasForeignKey(b => b.TechnicianId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Bookings)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(t => t.Certifications)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Optimistic Concurrency Token: Shadow Version property
        builder.Property<uint>("Version")
            .IsRowVersion()
            .IsConcurrencyToken();

        // Indexes
        builder.HasIndex(t => new { t.DealershipId, t.IsActive });
        builder.HasIndex(t => t.EmployeeNumber).IsUnique();

        builder.Ignore(t => t.DomainEvents);
    }
}

public sealed class TechnicianBookingConfiguration : IEntityTypeConfiguration<TechnicianBooking>
{
    public void Configure(EntityTypeBuilder<TechnicianBooking> builder)
    {
        builder.ToTable("TechnicianBookings");

        // Composite natural key: (TechnicianId, AppointmentId)
        builder.HasKey(b => new { b.TechnicianId, b.AppointmentId });

        builder.Property(b => b.TechnicianId)
            .HasConversion(StronglyTypedIdConverters.TechnicianIdConverter);

        builder.Property(b => b.AppointmentId)
            .HasConversion(StronglyTypedIdConverters.AppointmentIdConverter);

        builder.OwnsOne(b => b.Slot, slot =>
        {
            slot.Property(s => s.Start)
                .HasColumnName("StartTime")
                .IsRequired();

            slot.Property(s => s.End)
                .HasColumnName("EndTime")
                .IsRequired();

            slot.Ignore(s => s.Duration);

            // Index for overlap lookups
            slot.HasIndex(s => new { s.Start, s.End });
        });
    }
}
