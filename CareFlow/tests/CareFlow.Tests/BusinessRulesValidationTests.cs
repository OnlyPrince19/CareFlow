using CareFlow.Exceptions;
using CareFlow.Models;
using CareFlow.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CareFlow.Tests;

public class BusinessRulesValidationTests
{
    [Fact]
    public async Task BR01_BookingWithNonExistentPatient_ThrowsValidationException()
    {
        // Arrange
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;
        var service = new BookingService(context);

        var request = new CreateBookingRequest
        {
            PatientId = 99999, // Non-existent patient
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            SelectedServiceIds = new List<int> { 1 }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CareFlowValidationException>(() => service.CreateBookingAsync(request));
        Assert.Equal("BR-01", ex.RuleId);
        Assert.Contains("does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BR02_BookingWithEmptyServices_ThrowsValidationException()
    {
        // Arrange
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;
        var service = new BookingService(context);
        var patient = await context.Patients.FirstAsync();

        var request = new CreateBookingRequest
        {
            PatientId = patient.Id,
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            SelectedServiceIds = new List<int>() // 0 services
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CareFlowValidationException>(() => service.CreateBookingAsync(request));
        Assert.Equal("BR-02", ex.RuleId);
        Assert.Contains("between 1 and 3", ex.Message);
    }

    [Fact]
    public async Task BR02_BookingWithMoreThanThreeServices_ThrowsValidationException()
    {
        // Arrange
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;
        var service = new BookingService(context);
        var patient = await context.Patients.FirstAsync();

        var request = new CreateBookingRequest
        {
            PatientId = patient.Id,
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            SelectedServiceIds = new List<int> { 1, 2, 3, 4 } // 4 services
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CareFlowValidationException>(() => service.CreateBookingAsync(request));
        Assert.Equal("BR-02", ex.RuleId);
        Assert.Contains("between 1 and 3", ex.Message);
    }

    [Fact]
    public async Task BR03_BookingWithPastDate_ThrowsValidationException()
    {
        // Arrange
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;
        var service = new BookingService(context);
        var patient = await context.Patients.FirstAsync();

        var request = new CreateBookingRequest
        {
            PatientId = patient.Id,
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), // Yesterday
            SelectedServiceIds = new List<int> { 1 }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CareFlowValidationException>(() => service.CreateBookingAsync(request));
        Assert.Equal("BR-03", ex.RuleId);
        Assert.Contains("past", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BR06_BookingWithDuplicateServices_ThrowsValidationException()
    {
        // Arrange
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;
        var service = new BookingService(context);
        var patient = await context.Patients.FirstAsync();

        var request = new CreateBookingRequest
        {
            PatientId = patient.Id,
            BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            SelectedServiceIds = new List<int> { 1, 1 } // Duplicate service ID 1
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CareFlowValidationException>(() => service.CreateBookingAsync(request));
        Assert.Equal("BR-06", ex.RuleId);
        Assert.Contains("Duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BR05_BookingExceedingServiceDailyCapacity_ThrowsServiceCapacityExceededException()
    {
        // Arrange: ECG (Code: ECG) has capacity = 2 in seed data
        var (context, conn) = TestDatabaseFactory.CreateContext();
        await using var _ = conn;
        await using var __ = context;
        var service = new BookingService(context);
        var ecg = await context.DiagnosticServices.FirstAsync(s => s.Code == "ECG");
        var patients = await context.Patients.Take(3).ToListAsync();
        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        // Book slot 1
        await service.CreateBookingAsync(new CreateBookingRequest
        {
            PatientId = patients[0].Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { ecg.Id }
        });

        // Book slot 2 (capacity now 2/2 full)
        await service.CreateBookingAsync(new CreateBookingRequest
        {
            PatientId = patients[1].Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { ecg.Id }
        });

        // Act & Assert: Attempt slot 3 -> Must throw ServiceCapacityExceededException
        var ex = await Assert.ThrowsAsync<ServiceCapacityExceededException>(() => service.CreateBookingAsync(new CreateBookingRequest
        {
            PatientId = patients[2].Id,
            BookingDate = targetDate,
            SelectedServiceIds = new List<int> { ecg.Id }
        }));

        Assert.Equal("BR-05", ex.RuleId);
        Assert.Contains("maximum daily capacity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
