using Microsoft.EntityFrameworkCore;
using CareFlow.Data;
using CareFlow.Models;

namespace CareFlow.Services;

public class DiagnosticCatalogService : IDiagnosticCatalogService
{
    private readonly CareFlowDbContext _dbContext;

    public DiagnosticCatalogService(CareFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DiagnosticService>> GetAllServicesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.DiagnosticServices
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceAvailabilityDto>> GetAvailabilityForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var services = await _dbContext.DiagnosticServices
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .ToListAsync(cancellationToken);

        // Count existing bookings for each diagnostic service on the target date
        var bookedCounts = await _dbContext.BookingItems
            .AsNoTracking()
            .Where(bi => bi.Booking.BookingDate == date)
            .GroupBy(bi => bi.DiagnosticServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ServiceId, x => x.Count, cancellationToken);

        var result = new List<ServiceAvailabilityDto>(services.Count);

        foreach (var service in services)
        {
            var count = bookedCounts.TryGetValue(service.Id, out var booked) ? booked : 0;
            result.Add(new ServiceAvailabilityDto
            {
                ServiceId = service.Id,
                Code = service.Code,
                Name = service.Name,
                Department = service.Department,
                DailyCapacity = service.DailyCapacity,
                BookedCount = count
            });
        }

        return result;
    }
}
