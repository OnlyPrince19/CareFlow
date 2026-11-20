using CareFlow.Models;

namespace CareFlow.Services;

public interface IBookingService
{
    Task<BookingResult> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken cancellationToken = default);
    Task<Booking?> GetBookingByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Booking?> GetBookingByReferenceAsync(string reference, CancellationToken cancellationToken = default);
}
