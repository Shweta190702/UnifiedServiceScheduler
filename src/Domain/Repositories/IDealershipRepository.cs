using UnifiedServiceScheduler.Domain.Aggregates;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Repositories;

public interface IDealershipRepository
{
    Task<Dealership?> GetByIdAsync(DealershipId id, CancellationToken ct = default);
    Task AddAsync(Dealership dealership, CancellationToken ct = default);
    Task UpdateAsync(Dealership dealership, CancellationToken ct = default);
}
