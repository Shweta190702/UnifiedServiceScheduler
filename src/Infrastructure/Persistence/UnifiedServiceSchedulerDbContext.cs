using Microsoft.EntityFrameworkCore;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Entities;

namespace UnifiedServiceScheduler.Infrastructure.Persistence;

public class UnifiedServiceSchedulerDbContext : DbContext
{
    public UnifiedServiceSchedulerDbContext(DbContextOptions<UnifiedServiceSchedulerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Appointment>       Appointments       => Set<Appointment>();
    public DbSet<Technician>        Technicians        => Set<Technician>();
    public DbSet<TechnicianBooking> TechnicianBookings => Set<TechnicianBooking>();
    public DbSet<ServiceBay>        ServiceBays        => Set<ServiceBay>();
    public DbSet<BayBooking>        BayBookings        => Set<BayBooking>();
    public DbSet<Customer>          Customers          => Set<Customer>();
    public DbSet<Vehicle>           Vehicles           => Set<Vehicle>();
    public DbSet<ServiceType>       ServiceTypes       => Set<ServiceType>();
    public DbSet<Dealership>        Dealerships        => Set<Dealership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UnifiedServiceSchedulerDbContext).Assembly);
    }
}
