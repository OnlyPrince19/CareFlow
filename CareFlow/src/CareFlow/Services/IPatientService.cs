using CareFlow.Models;

namespace CareFlow.Services;

public interface IPatientService
{
    Task<IReadOnlyList<Patient>> GetAllPatientsAsync(CancellationToken cancellationToken = default);
    Task<Patient?> GetPatientByIdAsync(int id, CancellationToken cancellationToken = default);
}
