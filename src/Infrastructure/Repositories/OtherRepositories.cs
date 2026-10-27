using Microsoft.EntityFrameworkCore;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Repositories;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Infrastructure.Persistence;

namespace UnifiedServiceScheduler.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly UnifiedServiceSchedulerDbContext _context;

    public CustomerRepository(UnifiedServiceSchedulerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        await _context.Customers.AddAsync(customer, ct);
    }

    public Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        _context.Customers.Update(customer);
        return Task.CompletedTask;
    }
}

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly UnifiedServiceSchedulerDbContext _context;

    public VehicleRepository(UnifiedServiceSchedulerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        await _context.Vehicles.AddAsync(vehicle, ct);
    }

    public Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        _context.Vehicles.Update(vehicle);
        return Task.CompletedTask;
    }
}

public sealed class DealershipRepository : IDealershipRepository
{
    private readonly UnifiedServiceSchedulerDbContext _context;

    public DealershipRepository(UnifiedServiceSchedulerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Dealership?> GetByIdAsync(DealershipId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await _context.Dealerships.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task AddAsync(Dealership dealership, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dealership);
        await _context.Dealerships.AddAsync(dealership, ct);
    }

    public Task UpdateAsync(Dealership dealership, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dealership);
        _context.Dealerships.Update(dealership);
        return Task.CompletedTask;
    }
}

public sealed class ServiceTypeRepository : IServiceTypeRepository
{
    private readonly UnifiedServiceSchedulerDbContext _context;

    public ServiceTypeRepository(UnifiedServiceSchedulerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ServiceType?> GetByIdAsync(ServiceTypeId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await _context.ServiceTypes.FirstOrDefaultAsync(st => st.Id == id, ct);
    }

    public async Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.ServiceTypes.ToListAsync(ct);
    }

    public async Task AddAsync(ServiceType serviceType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        await _context.ServiceTypes.AddAsync(serviceType, ct);
    }
}
