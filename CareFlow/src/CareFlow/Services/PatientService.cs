using Microsoft.EntityFrameworkCore;
using CareFlow.Data;
using CareFlow.Models;

namespace CareFlow.Services;

public class PatientService : IPatientService
{
    private readonly CareFlowDbContext _dbContext;

    public PatientService(CareFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Patient>> GetAllPatientsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Patients
            .AsNoTracking()
            .OrderBy(p => p.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Patient?> GetPatientByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
