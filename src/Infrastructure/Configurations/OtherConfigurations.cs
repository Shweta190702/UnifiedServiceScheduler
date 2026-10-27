using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Infrastructure.Persistence;

namespace UnifiedServiceScheduler.Infrastructure.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(StronglyTypedIdConverters.CustomerIdConverter)
            .ValueGeneratedNever();

        builder.OwnsOne(c => c.Contact, contact =>
        {
            contact.Property(p => p.FirstName).HasColumnName("FirstName").HasMaxLength(100).IsRequired();
            contact.Property(p => p.LastName).HasColumnName("LastName").HasMaxLength(100).IsRequired();
            contact.Property(p => p.Email).HasColumnName("Email").HasMaxLength(255).IsRequired();
            contact.Property(p => p.PhoneNumber).HasColumnName("PhoneNumber").HasMaxLength(50).IsRequired();
            contact.Ignore(p => p.FullName);

            contact.HasIndex(p => p.Email);
        });
    }
}

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasConversion(StronglyTypedIdConverters.VehicleIdConverter)
            .ValueGeneratedNever();

        builder.Property(v => v.OwnerId)
            .HasConversion(StronglyTypedIdConverters.CustomerIdConverter)
            .IsRequired();

        builder.Property(v => v.Make).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Year).IsRequired();
        builder.Property(v => v.Vin).HasMaxLength(17).IsFixedLength().IsRequired();
        builder.Property(v => v.LicensePlate).HasMaxLength(20).IsRequired();

        builder.HasIndex(v => v.Vin).IsUnique();
        builder.HasIndex(v => v.OwnerId);
    }
}

public sealed class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.ToTable("ServiceTypes");

        builder.HasKey(st => st.Id);
        builder.Property(st => st.Id)
            .HasConversion(StronglyTypedIdConverters.ServiceTypeIdConverter)
            .ValueGeneratedNever();

        builder.Property(st => st.Name).HasMaxLength(100).IsRequired();
        builder.Property(st => st.ServiceCategory).HasMaxLength(100).IsRequired();
        builder.Property(st => st.BaseDuration).IsRequired();
        builder.Property(st => st.RequiredSkillLevel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(st => st.RequiredBayCapability).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(st => st.Description).HasMaxLength(500);

        builder.HasIndex(st => st.ServiceCategory);
    }
}

public sealed class DealershipConfiguration : IEntityTypeConfiguration<Dealership>
{
    public void Configure(EntityTypeBuilder<Dealership> builder)
    {
        builder.ToTable("Dealerships");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(StronglyTypedIdConverters.DealershipIdConverter)
            .ValueGeneratedNever();

        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.Property(d => d.Address).HasMaxLength(250).IsRequired();

        builder.Property(d => d.TimeZone)
            .HasConversion(tz => tz.Id, id => TimeZoneInfo.FindSystemTimeZoneById(id))
            .HasMaxLength(100)
            .IsRequired();

        builder.Ignore(d => d.Schedule);
        builder.Ignore(d => d.DomainEvents);
    }
}
