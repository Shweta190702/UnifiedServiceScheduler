using Microsoft.EntityFrameworkCore;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Repositories;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Infrastructure.Persistence;

namespace UnifiedServiceScheduler.Infrastructure.Repositories;

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly UnifiedServiceSchedulerDbContext _context;

    public AppointmentRepository(UnifiedServiceSchedulerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Appointment?> GetByIdAsync(AppointmentId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        return await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<IReadOnlyList<Appointment>> FindByDealershipAndSlotAsync(
        DealershipId      dealershipId,
        TimeSlot          window,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentNullException.ThrowIfNull(window);

        // Bounded time window query: only appointments intersecting the requested window are loaded
        return await _context.Appointments
            .Where(a => a.DealershipId == dealershipId)
            .Where(a => a.Slot.Start < window.End && a.Slot.End > window.Start)
            .ToListAsync(ct);
    }

    public async Task<Appointment?> FindDuplicateAsync(
        DealershipId      dealershipId,
        CustomerId        customerId,
        VehicleId         vehicleId,
        TimeSlot          slot,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentNullException.ThrowIfNull(customerId);
        ArgumentNullException.ThrowIfNull(vehicleId);
        ArgumentNullException.ThrowIfNull(slot);

        return await _context.Appointments
            .Where(a => a.DealershipId == dealershipId && a.CustomerId == customerId)
            .Where(a => a.Slot.Start < slot.End && a.Slot.End > slot.Start)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        await _context.Appointments.AddAsync(appointment, ct);
    }

    public Task UpdateAsync(Appointment appointment, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        _context.Appointments.Update(appointment);
        return Task.CompletedTask;
    }
}
