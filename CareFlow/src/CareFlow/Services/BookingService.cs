using Microsoft.EntityFrameworkCore;
using CareFlow.Data;
using CareFlow.Exceptions;
using CareFlow.Models;

namespace CareFlow.Services;

public class BookingService : IBookingService
{
    private readonly CareFlowDbContext _dbContext;

    public BookingService(CareFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BookingResult> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        // BR-01: Each booking must reference an existing patient
        var patientExists = await _dbContext.Patients
            .AnyAsync(p => p.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            throw new CareFlowValidationException("BR-01", $"Patient with ID {request.PatientId} does not exist in the system.");
        }

        // BR-02: Each booking must contain one to three distinct diagnostic services
        if (request.SelectedServiceIds == null || request.SelectedServiceIds.Count < 1 || request.SelectedServiceIds.Count > 3)
        {
            throw new CareFlowValidationException("BR-02", "A booking must contain between 1 and 3 diagnostic services.");
        }

        // BR-06: The same service cannot appear more than once in a booking request
        if (request.SelectedServiceIds.Distinct().Count() != request.SelectedServiceIds.Count)
        {
            throw new CareFlowValidationException("BR-06", "Duplicate services detected. The same diagnostic service cannot appear more than once in a booking request.");
        }

        // BR-03: The booking date cannot be earlier than the current local date
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (request.BookingDate < today)
        {
            throw new CareFlowValidationException("BR-03", $"Invalid booking date ({request.BookingDate:yyyy-MM-dd}). Bookings cannot be scheduled for past dates. Current local date is {today:yyyy-MM-dd}.");
        }

        // BR-07: All required booking changes must be committed together, or none must be committed (Coordinated DB Operation / Transaction)
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // BR-08: Availability must be rechecked when the booking is confirmed, rather than relying solely on previously displayed information
            // BR-04: Each selected service consumes one unit of its daily booking capacity
            // BR-05: Confirmed bookings cannot exceed the daily capacity of a diagnostic service
            foreach (var serviceId in request.SelectedServiceIds)
            {
                var service = await _dbContext.DiagnosticServices
                    .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);

                if (service == null)
                {
                    throw new CareFlowValidationException("BR-01", $"Diagnostic service with ID {serviceId} was not found.");
                }

                // Query current confirmed booking count for this service on target date
                var currentBookedCount = await _dbContext.BookingItems
                    .CountAsync(bi => bi.DiagnosticServiceId == serviceId && bi.Booking.BookingDate == request.BookingDate, cancellationToken);

                if (currentBookedCount + 1 > service.DailyCapacity)
                {
                    // BR-05 & BR-10: Reject entire request and roll back
                    throw new ServiceCapacityExceededException(service.Id, service.Name, request.BookingDate);
                }
            }

            // Generate unique human-readable booking reference
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var bookingReference = $"CF-{request.BookingDate:yyyyMMdd}-{suffix}";

            var booking = new Booking
            {
                BookingReference = bookingReference,
                PatientId = request.PatientId,
                BookingDate = request.BookingDate,
                CreatedAt = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                Notes = request.Notes?.Trim()
            };

            foreach (var serviceId in request.SelectedServiceIds)
            {
                booking.Items.Add(new BookingItem
                {
                    DiagnosticServiceId = serviceId
                });
            }

            await _dbContext.Bookings.AddAsync(booking, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Controlled failure scenario for AC-05 verification
            if (request.SimulateFailureBeforeCommit)
            {
                throw new InvalidOperationException("AC-05 SIMULATED FAILURE: Artificial database operation failure triggered immediately prior to transaction commit to verify atomic rollback.");
            }

            // Commit atomic transaction
            await transaction.CommitAsync(cancellationToken);

            // Fetch persisted entity with navigation properties for UI display
            var persistedBooking = await _dbContext.Bookings
                .Include(b => b.Patient)
                .Include(b => b.Items)
                    .ThenInclude(bi => bi.DiagnosticService)
                .FirstAsync(b => b.Id == booking.Id, cancellationToken);

            return BookingResult.Success(persistedBooking);
        }
        catch
        {
            // Transaction boundary ensures full rollback on any failure
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Items)
                .ThenInclude(bi => bi.DiagnosticService)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Booking?> GetBookingByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Items)
                .ThenInclude(bi => bi.DiagnosticService)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Booking?> GetBookingByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Patient)
            .Include(b => b.Items)
                .ThenInclude(bi => bi.DiagnosticService)
            .FirstOrDefaultAsync(b => b.BookingReference == reference, cancellationToken);
    }
}
