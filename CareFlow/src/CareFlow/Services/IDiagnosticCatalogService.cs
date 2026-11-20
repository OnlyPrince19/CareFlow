using CareFlow.Models;

namespace CareFlow.Services;

public interface IDiagnosticCatalogService
{
    Task<IReadOnlyList<ServiceAvailabilityDto>> GetAvailabilityForDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiagnosticService>> GetAllServicesAsync(CancellationToken cancellationToken = default);
}
