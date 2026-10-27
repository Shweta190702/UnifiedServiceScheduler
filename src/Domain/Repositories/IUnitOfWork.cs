namespace UnifiedServiceScheduler.Domain.Repositories;

/// <summary>
/// Atomic persistence boundary contract.
/// Ensures that changes across Appointment, Technician, and ServiceBay aggregates
/// within a single scheduling transaction are committed together.
/// </summary>
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct = default);
}
