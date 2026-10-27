using Microsoft.EntityFrameworkCore;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.Repositories;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Infrastructure.Persistence;

namespace UnifiedServiceScheduler.Infrastructure.Repositories;

public sealed class ServiceBayRepository : IServiceBayRepository
{
    private readonly UnifiedServiceSchedulerDbContext _context;

    public ServiceBayRepository(UnifiedServiceSchedulerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ServiceBay?> GetByIdAsync(ServiceBayId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return await _context.ServiceBays
            .Include(b => b.Bookings)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    /// <summary>
    /// Acquires a pessimistic row-level lock on the ServiceBay record via SELECT ... FOR UPDATE.
    /// Serializes concurrent transactions attempting to book the same bay simultaneously.
    /// </summary>
    public async Task<ServiceBay?> GetByIdWithLockAsync(ServiceBayId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var bay = await _context.ServiceBays
            .FromSqlInterpolated($"SELECT * FROM \"ServiceBays\" WHERE \"Id\" = {id.Value} FOR UPDATE")
            .FirstOrDefaultAsync(ct);

        if (bay is not null)
        {
            await _context.Entry(bay).Collection(b => b.Bookings).LoadAsync(ct);
        }

        return bay;
    }

    /// <summary>
    /// Pushes the overlap check and capability filter directly into the database engine.
    /// Translates to SQL:
    /// WHERE b.DealershipId = @d AND b.Capability = @cap AND b.IsUnderMaintenance = false
    ///   AND NOT EXISTS (SELECT 1 FROM BayBookings bb WHERE bb.StartTime < @slotEnd AND bb.EndTime > @slotStart)
    /// </summary>
    public async Task<IReadOnlyList<ServiceBay>> FindAvailableAsync(
        DealershipId      dealershipId,
        BayCapability     requiredCapability,
        TimeSlot          requestedSlot,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentNullException.ThrowIfNull(requestedSlot);

        var query = _context.ServiceBays
            .Include(b => b.Bookings)
            .Where(b => b.DealershipId == dealershipId &&
                        !b.IsUnderMaintenance &&
                        b.Capability == requiredCapability)
            // Non-overlapping schedule filter pushed to DB:
            .Where(b => !b.Bookings.Any(bb =>
                bb.Slot.Start < requestedSlot.End &&
                bb.Slot.End > requestedSlot.Start));

        return await query.ToListAsync(ct);
    }

    public async Task<bool> HasCompatibleBayAsync(
        DealershipId      dealershipId,
        BayCapability     requiredCapability,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dealershipId);

        return await _context.ServiceBays
            .AnyAsync(b => b.DealershipId == dealershipId &&
                           !b.IsUnderMaintenance &&
                           b.Capability == requiredCapability, ct);
    }

    public async Task AddAsync(ServiceBay bay, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bay);
        await _context.ServiceBays.AddAsync(bay, ct);
    }

    public Task UpdateAsync(ServiceBay bay, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bay);
        _context.ServiceBays.Update(bay);
        return Task.CompletedTask;
    }
}
