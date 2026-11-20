using Microsoft.EntityFrameworkCore;
using CareFlow.Data;
using CareFlow.Exceptions;
using CareFlow.Models;
using CareFlow.Services;
using Xunit;

namespace CareFlow.Tests;

public class AcceptanceCriteriaTests
{
    [Fact]
    public async Task AC01_ViewDiagnosticServices_CalculatesRemainingCapacityCorrectly()
    {
        // AC-01: View diagnostic services for a selected date. Remaining capacity is correctly calculated from persisted information.
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;

        var catalogService = new DiagnosticCatalogService(context);
        var bookingService = new BookingService(context);
        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        var cbc = await context.DiagnosticServices.FirstAsync(s => s.Code == "CBC"); // DailyCapacity: 5
        var patient = await context.Patients.FirstAsync();

        // Initial check: 5 remaining
        var initialList = await catalogService.GetAvailabilityForDateAsync(targetDate);
        var initialCbc = initialList.First(s => s.Code == "CBC");
        Assert.Equal(5, initialCbc.DailyCapacity);
        Assert.Equal(0, initialCbc.BookedCount);
        Assert.Equal(5, initialCbc.RemainingCapacity);

        // Book 1 CBC
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            PatientId = patient.Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { cbc.Id }
        });

        // After booking: 1 booked, 4 remaining
        var updatedList = await catalogService.GetAvailabilityForDateAsync(targetDate);
        var updatedCbc = updatedList.First(s => s.Code == "CBC");
        Assert.Equal(1, updatedCbc.BookedCount);
        Assert.Equal(4, updatedCbc.RemainingCapacity);
    }

    [Fact]
    public async Task AC02_CreateBookingWithMultipleServices_RecordsOneConfirmedBooking()
    {
        // AC-02: Create a booking containing multiple available services. One confirmed booking is recorded, including every selected service.
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;

        var bookingService = new BookingService(context);
        var patient = await context.Patients.FirstAsync();
        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(6));

        var cbc = await context.DiagnosticServices.FirstAsync(s => s.Code == "CBC");
        var urn = await context.DiagnosticServices.FirstAsync(s => s.Code == "URN");
        var cxr = await context.DiagnosticServices.FirstAsync(s => s.Code == "CXR");

        var request = new CreateBookingRequest
        {
            PatientId = patient.Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { cbc.Id, urn.Id, cxr.Id },
            Notes = "Multi-service pre-employment checkup"
        };

        var result = await bookingService.CreateBookingAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Booking);
        Assert.Equal(BookingStatus.Confirmed, result.Booking.Status);
        Assert.Equal(3, result.Booking.Items.Count);

        // Verify in DB directly
        var persisted = await context.Bookings
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.BookingReference == result.BookingReference);

        Assert.NotNull(persisted);
        Assert.Equal(patient.Id, persisted.PatientId);
        Assert.Equal(3, persisted.Items.Count);
        Assert.Contains(persisted.Items, item => item.DiagnosticServiceId == cbc.Id);
        Assert.Contains(persisted.Items, item => item.DiagnosticServiceId == urn.Id);
        Assert.Contains(persisted.Items, item => item.DiagnosticServiceId == cxr.Id);
    }

    [Fact]
    public async Task AC03_RequestFullyBookedServiceOrInvalidInfo_IsRejectedWithoutIncompleteRecords()
    {
        // AC-03: Request a fully booked service or submit invalid booking information.
        // The request is rejected without creating incomplete or invalid records.
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;

        var bookingService = new BookingService(context);
        var ecg = await context.DiagnosticServices.FirstAsync(s => s.Code == "ECG"); // Capacity: 2
        var cbc = await context.DiagnosticServices.FirstAsync(s => s.Code == "CBC");
        var patients = await context.Patients.Take(4).ToListAsync();
        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        // Fill ECG to capacity (2 slots)
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            PatientId = patients[0].Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { ecg.Id }
        });
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            PatientId = patients[1].Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { ecg.Id }
        });

        var initialBookingsCount = await context.Bookings.CountAsync();
        var initialBookingItemsCount = await context.BookingItems.CountAsync();

        // Attempt booking CBC and ECG (ECG is full, CBC is available)
        // Must reject entire request, rather than recording CBC separately!
        var multiRequest = new CreateBookingRequest
        {
            PatientId = patients[2].Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { cbc.Id, ecg.Id }
        };

        await Assert.ThrowsAsync<ServiceCapacityExceededException>(() => bookingService.CreateBookingAsync(multiRequest));

        // Verify NO partial records or incomplete records were written
        Assert.Equal(initialBookingsCount, await context.Bookings.CountAsync());
        Assert.Equal(initialBookingItemsCount, await context.BookingItems.CountAsync());
    }

    [Fact]
    public async Task AC04_AttemptBookingAfterCapacityBecomesUnavailable_RevalidatesAndPreventsOverbooking()
    {
        // AC-04: Attempt a booking after previously displayed capacity becomes unavailable.
        // Availability is revalidated, and the system prevents an overbooking.
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;

        var catalogService = new DiagnosticCatalogService(context);
        var bookingService = new BookingService(context);
        var ecg = await context.DiagnosticServices.FirstAsync(s => s.Code == "ECG"); // Capacity: 2
        var patients = await context.Patients.Take(3).ToListAsync();
        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8));

        // Step 1: Staff reads availability when 1 slot remains
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            PatientId = patients[0].Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { ecg.Id }
        });

        var staffViewedAvailability = await catalogService.GetAvailabilityForDateAsync(targetDate);
        var ecgViewed = staffViewedAvailability.First(s => s.Code == "ECG");
        Assert.Equal(1, ecgViewed.RemainingCapacity); // Staff sees 1 slot remaining

        // Step 2: Concurrently, another booking takes the last slot
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            PatientId = patients[1].Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { ecg.Id }
        });

        // Step 3: Staff submits their stale booking attempt
        var staleSubmission = new CreateBookingRequest
        {
            PatientId = patients[2].Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { ecg.Id }
        };

        // System must re-verify availability inside the transaction and reject it
        var ex = await Assert.ThrowsAsync<ServiceCapacityExceededException>(() => bookingService.CreateBookingAsync(staleSubmission));
        Assert.Equal("BR-05", ex.RuleId);

        // Verify total confirmed bookings for ECG on targetDate strictly equals 2 (not 3)
        var finalBookedCount = await context.BookingItems
            .CountAsync(bi => bi.DiagnosticServiceId == ecg.Id && bi.Booking.BookingDate == targetDate);
        Assert.Equal(2, finalBookedCount);
    }

    [Fact]
    public async Task AC05_DatabaseOperationFailsBeforeCommit_RollsBackWithoutPartialRecordsOrCapacityChange()
    {
        // AC-05: A required database operation fails before a booking is committed.
        // No partial booking or incorrect capacity change is persisted.
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;

        var bookingService = new BookingService(context);
        var patient = await context.Patients.FirstAsync();
        var cbc = await context.DiagnosticServices.FirstAsync(s => s.Code == "CBC");
        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(9));

        var initialBookingsCount = await context.Bookings.CountAsync();
        var initialBookingItemsCount = await context.BookingItems.CountAsync();

        var failingRequest = new CreateBookingRequest
        {
            PatientId = patient.Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { cbc.Id },
            SimulateFailureBeforeCommit = true // Triggers exception right before transaction.CommitAsync()
        };

        // Assert failure occurred
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => bookingService.CreateBookingAsync(failingRequest));
        Assert.Contains("AC-05 SIMULATED FAILURE", ex.Message);

        // Assert transaction was rolled back: no booking or booking items created
        var finalBookingsCount = await context.Bookings.CountAsync();
        var finalBookingItemsCount = await context.BookingItems.CountAsync();

        Assert.Equal(initialBookingsCount, finalBookingsCount);
        Assert.Equal(initialBookingItemsCount, finalBookingItemsCount);

        // Capacity is also completely unchanged
        var cbcBookedCount = await context.BookingItems
            .CountAsync(bi => bi.DiagnosticServiceId == cbc.Id && bi.Booking.BookingDate == targetDate);
        Assert.Equal(0, cbcBookedCount);
    }

    [Fact]
    public async Task AC06_RestartApplication_PreviouslyRecordedBookingsPersist()
    {
        // AC-06: Close and restart the application after confirming a booking.
        // Previously recorded bookings and their associated services remain available.
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"careflow_test_{Guid.NewGuid():N}.db");

        try
        {
            string createdBookingRef;
            DateOnly targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
            int patientId;
            int serviceId;

            // Session 1: Create booking and dispose context (simulates closing the app)
            {
                var options = new DbContextOptionsBuilder<CareFlowDbContext>()
                    .UseSqlite($"Data Source={tempDbPath}")
                    .Options;

                await using var session1Context = new CareFlowDbContext(options);
                await DbInitializer.InitializeAsync(session1Context);

                var patient = await session1Context.Patients.FirstAsync();
                var service = await session1Context.DiagnosticServices.FirstAsync();
                patientId = patient.Id;
                serviceId = service.Id;

                var bookingService = new BookingService(session1Context);
                var result = await bookingService.CreateBookingAsync(new CreateBookingRequest
                {
                    PatientId = patientId,
                    BookingDate = targetDate,
                    SelectedServiceIds = new List<int> { serviceId },
                    Notes = "Testing session restart persistence"
                });

                createdBookingRef = result.BookingReference!;
                Assert.NotNull(createdBookingRef);
            }

            // Session 2: Fresh start with a brand new context pointing to the same SQLite DB file
            {
                var options = new DbContextOptionsBuilder<CareFlowDbContext>()
                    .UseSqlite($"Data Source={tempDbPath}")
                    .Options;

                await using var session2Context = new CareFlowDbContext(options);
                var bookingService = new BookingService(session2Context);

                var retrievedBooking = await bookingService.GetBookingByReferenceAsync(createdBookingRef);

                Assert.NotNull(retrievedBooking);
                Assert.Equal(createdBookingRef, retrievedBooking.BookingReference);
                Assert.Equal(patientId, retrievedBooking.PatientId);
                Assert.Equal(targetDate, retrievedBooking.BookingDate);
                Assert.Single(retrievedBooking.Items);
                Assert.Equal(serviceId, retrievedBooking.Items.First().DiagnosticServiceId);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(tempDbPath))
            {
                File.Delete(tempDbPath);
            }
        }
    }
}
