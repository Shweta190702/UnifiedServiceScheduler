using Microsoft.EntityFrameworkCore;
using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.Enums;
using UnifiedServiceScheduler.Domain.Repositories;
using UnifiedServiceScheduler.Domain.ValueObjects;
using UnifiedServiceScheduler.Infrastructure.Persistence;

namespace UnifiedServiceScheduler.Infrastructure.Repositories;

public sealed class TechnicianRepository : ITechnicianRepository
{
    private readonly UnifiedServiceSchedulerDbContext _context;

    public TechnicianRepository(UnifiedServiceSchedulerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Technician?> GetByIdAsync(TechnicianId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return await _context.Technicians
            .Include(t => t.Bookings)
            .Include(t => t.Certifications)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <summary>
    /// Acquires a pessimistic row-level lock on the Technician record via SELECT ... FOR UPDATE.
    /// Serializes concurrent transactions attempting to book the same technician simultaneously.
    /// </summary>
    public async Task<Technician?> GetByIdWithLockAsync(TechnicianId id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        // Raw SQL with FOR UPDATE ensures the database locks this specific row until transaction end
        var tech = await _context.Technicians
            .FromSqlInterpolated($"SELECT * FROM \"Technicians\" WHERE \"Id\" = {id.Value} FOR UPDATE")
            .FirstOrDefaultAsync(ct);

        if (tech is not null)
        {
            // Explicitly load collections on the locked instance
            await _context.Entry(tech).Collection(t => t.Bookings).LoadAsync(ct);
            await _context.Entry(tech).Collection(t => t.Certifications).LoadAsync(ct);
        }

        return tech;
    }

    /// <summary>
    /// Pushes the overlap check directly into the database engine.
    /// Translates to SQL:
    /// WHERE t.DealershipId = @d AND t.IsActive = true
    ///   AND EXISTS (SELECT 1 FROM Certifications c WHERE c.Category = @cat AND c.Level >= @min AND ...)
    ///   AND NOT EXISTS (SELECT 1 FROM Bookings b WHERE b.StartTime < @slotEnd AND b.EndTime > @slotStart)
    /// </summary>
    public async Task<IReadOnlyList<Technician>> FindAvailableAsync(
        DealershipId      dealershipId,
        string            serviceCategory,
        SkillLevel        minimumSkillLevel,
        DateOnly          certificationAsOf,
        TimeSlot          requestedSlot,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceCategory);
        ArgumentNullException.ThrowIfNull(requestedSlot);

        var query = _context.Technicians
            .Include(t => t.Bookings)
            .Include(t => t.Certifications)
            .Where(t => t.DealershipId == dealershipId && t.IsActive)
            // Qualification filter pushed to DB:
            .Where(t => t.Certifications.Any(c =>
                c.ServiceCategory == serviceCategory &&
                c.Level >= minimumSkillLevel &&
                (c.ExpiresOn == null || c.ExpiresOn >= certificationAsOf)))
            // Non-overlapping schedule filter pushed to DB (eliminates in-memory filtering OOM):
            .Where(t => !t.Bookings.Any(b =>
                b.Slot.Start < requestedSlot.End &&
                b.Slot.End > requestedSlot.Start));

        return await query.ToListAsync(ct);
    }

    public async Task<bool> HasQualifiedTechniciansAsync(
        DealershipId      dealershipId,
        string            serviceCategory,
        SkillLevel        minimumSkillLevel,
        DateOnly          certificationAsOf,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dealershipId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceCategory);

        return await _context.Technicians
            .Where(t => t.DealershipId == dealershipId && t.IsActive)
            .AnyAsync(t => t.Certifications.Any(c =>
                c.ServiceCategory == serviceCategory &&
                c.Level >= minimumSkillLevel &&
                (c.ExpiresOn == null || c.ExpiresOn >= certificationAsOf)), ct);
    }

    public async Task AddAsync(Technician technician, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(technician);
        await _context.Technicians.AddAsync(technician, ct);
    }

    public Task UpdateAsync(Technician technician, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(technician);
        _context.Technicians.Update(technician);
        return Task.CompletedTask;
    }
}
