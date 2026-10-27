using UnifiedServiceScheduler.Domain.Entities;
using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Repositories;

public interface IServiceTypeRepository
{
    Task<ServiceType?> GetByIdAsync(ServiceTypeId id, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(ServiceType serviceType, CancellationToken ct = default);
}
