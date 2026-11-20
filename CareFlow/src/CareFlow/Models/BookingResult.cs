namespace CareFlow.Models;

public class BookingResult
{
    public bool IsSuccess { get; set; }
    public string? BookingReference { get; set; }
    public Booking? Booking { get; set; }
    public string Message { get; set; } = string.Empty;

    public static BookingResult Success(Booking booking) => new()
    {
        IsSuccess = true,
        BookingReference = booking.BookingReference,
        Booking = booking,
        Message = $"Booking {booking.BookingReference} has been confirmed successfully!"
    };

    public static BookingResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        Message = errorMessage
    };
}
