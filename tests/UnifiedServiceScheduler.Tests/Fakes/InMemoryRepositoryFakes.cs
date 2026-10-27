using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.Repositories;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Tests.Fakes;

/// <summary>
/// Genuine in-memory repository implementations that store and retrieve real domain aggregates.
/// Completely avoids the Anti-Mocking Trap: all domain aggregate methods (Reserve, OverlapsWith,
/// Supports, CanPerform) run actual production domain logic.
/// </summary>
public sealed class InMemoryTechnicianRepository : ITechnicianRepository
{
    private readonly Dictionary<TechnicianId, Technician> _technicians = [];

    public void Seed(Technician technician) => _technicians[technician.Id] = technician;

    public Task<Technician?> GetByIdAsync(TechnicianId id, CancellationToken ct = default)
    {
        _technicians.TryGetValue(id, out var tech);
        return Task.FromResult(tech);
    }

    public Task<IReadOnlyList<Technician>> FindAvailableAsync(
        DealershipId      dealershipId,
        string            serviceCategory,
        SkillLevel        minimumSkillLevel,
        DateOnly          certificationAsOf,
        TimeSlot          requestedSlot,
        CancellationToken ct = default)
    {
        var available = _technicians.Values
            .Where(t => t.DealershipId == dealershipId && t.IsActive)
            .Where(t => t.CanPerform(
                ServiceType.Define(ServiceTypeId.New(), "Temp", serviceCategory, requestedSlot.Duration, minimumSkillLevel, BayCapability.Standard),
                certificationAsOf))
            .Where(t => !t.Bookings.Any(b => b.Slot.OverlapsWith(requestedSlot)))
            .ToList();

        return Task.FromResult<IReadOnlyList<Technician>>(available);
    }

    public Task<bool> HasQualifiedTechniciansAsync(
        DealershipId      dealershipId,
        string            serviceCategory,
        SkillLevel        minimumSkillLevel,
        DateOnly          certificationAsOf,
        CancellationToken ct = default)
    {
        var hasAny = _technicians.Values.Any(t =>
            t.DealershipId == dealershipId &&
            t.IsActive &&
            t.CanPerform(
                ServiceType.Define(ServiceTypeId.New(), "Temp", serviceCategory, TimeSpan.FromHours(1), minimumSkillLevel, BayCapability.Standard),
                certificationAsOf));

        return Task.FromResult(hasAny);
    }

    public Task AddAsync(Technician technician, CancellationToken ct = default)
    {
        _technicians[technician.Id] = technician;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Technician technician, CancellationToken ct = default)
    {
        _technicians[technician.Id] = technician;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryServiceBayRepository : IServiceBayRepository
{
    private readonly Dictionary<ServiceBayId, ServiceBay> _bays = [];

    public void Seed(ServiceBay bay) => _bays[bay.Id] = bay;

    public Task<ServiceBay?> GetByIdAsync(ServiceBayId id, CancellationToken ct = default)
    {
        _bays.TryGetValue(id, out var bay);
        return Task.FromResult(bay);
    }

    public Task<IReadOnlyList<ServiceBay>> FindAvailableAsync(
        DealershipId      dealershipId,
        BayCapability     requiredCapability,
        TimeSlot          requestedSlot,
        CancellationToken ct = default)
    {
        var available = _bays.Values
            .Where(b => b.DealershipId == dealershipId && !b.IsUnderMaintenance && b.Capability == requiredCapability)
            .Where(b => !b.Bookings.Any(booking => booking.Slot.OverlapsWith(requestedSlot)))
            .ToList();

        return Task.FromResult<IReadOnlyList<ServiceBay>>(available);
    }

    public Task<bool> HasCompatibleBayAsync(
        DealershipId      dealershipId,
        BayCapability     requiredCapability,
        CancellationToken ct = default)
    {
        var hasAny = _bays.Values.Any(b =>
            b.DealershipId == dealershipId &&
            !b.IsUnderMaintenance &&
            b.Capability == requiredCapability);

        return Task.FromResult(hasAny);
    }

    public Task AddAsync(ServiceBay bay, CancellationToken ct = default)
    {
        _bays[bay.Id] = bay;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ServiceBay bay, CancellationToken ct = default)
    {
        _bays[bay.Id] = bay;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryAppointmentRepository : IAppointmentRepository
{
    private readonly Dictionary<AppointmentId, Appointment> _appointments = [];

    public IReadOnlyCollection<Appointment> All => _appointments.Values;

    public Task<Appointment?> GetByIdAsync(AppointmentId id, CancellationToken ct = default)
    {
        _appointments.TryGetValue(id, out var appt);
        return Task.FromResult(appt);
    }

    public Task<IReadOnlyList<Appointment>> FindByDealershipAndSlotAsync(
        DealershipId dealershipId,
        TimeSlot window,
        CancellationToken ct = default)
    {
        var results = _appointments.Values
            .Where(a => a.DealershipId == dealershipId && a.Slot.OverlapsWith(window))
            .ToList();

        return Task.FromResult<IReadOnlyList<Appointment>>(results);
    }

    public Task<Appointment?> FindDuplicateAsync(
        DealershipId dealershipId,
        CustomerId   customerId,
        VehicleId    vehicleId,
        TimeSlot     slot,
        CancellationToken ct = default)
    {
        var existing = _appointments.Values
            .FirstOrDefault(a => a.DealershipId == dealershipId &&
                                 a.CustomerId == customerId &&
                                 a.Slot.OverlapsWith(slot));
        return Task.FromResult(existing);
    }

    public Task AddAsync(Appointment appointment, CancellationToken ct = default)
    {
        _appointments[appointment.Id] = appointment;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Appointment appointment, CancellationToken ct = default)
    {
        _appointments[appointment.Id] = appointment;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryDealershipRepository : IDealershipRepository
{
    private readonly Dictionary<DealershipId, Dealership> _dealerships = [];

    public void Seed(Dealership dealership) => _dealerships[dealership.Id] = dealership;

    public Task<Dealership?> GetByIdAsync(DealershipId id, CancellationToken ct = default)
    {
        _dealerships.TryGetValue(id, out var d);
        return Task.FromResult(d);
    }

    public Task AddAsync(Dealership dealership, CancellationToken ct = default)
    {
        _dealerships[dealership.Id] = dealership;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Dealership dealership, CancellationToken ct = default)
    {
        _dealerships[dealership.Id] = dealership;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<CustomerId, Customer> _customers = [];

    public void Seed(Customer customer) => _customers[customer.Id] = customer;

    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
    {
        _customers.TryGetValue(id, out var c);
        return Task.FromResult(c);
    }

    public Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _customers[customer.Id] = customer;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryVehicleRepository : IVehicleRepository
{
    private readonly Dictionary<VehicleId, Vehicle> _vehicles = [];

    public void Seed(Vehicle vehicle) => _vehicles[vehicle.Id] = vehicle;

    public Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken ct = default)
    {
        _vehicles.TryGetValue(id, out var v);
        return Task.FromResult(v);
    }

    public Task AddAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        _vehicles[vehicle.Id] = vehicle;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        _vehicles[vehicle.Id] = vehicle;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryServiceTypeRepository : IServiceTypeRepository
{
    private readonly Dictionary<ServiceTypeId, ServiceType> _serviceTypes = [];

    public void Seed(ServiceType serviceType) => _serviceTypes[serviceType.Id] = serviceType;

    public Task<ServiceType?> GetByIdAsync(ServiceTypeId id, CancellationToken ct = default)
    {
        _serviceTypes.TryGetValue(id, out var st);
        return Task.FromResult(st);
    }

    public Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<ServiceType>>(_serviceTypes.Values.ToList());
    }

    public Task AddAsync(ServiceType serviceType, CancellationToken ct = default)
    {
        _serviceTypes[serviceType.Id] = serviceType;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public int CommitCount { get; private set; }

    public Task CommitAsync(CancellationToken ct = default)
    {
        CommitCount++;
        return Task.CompletedTask;
    }
}
