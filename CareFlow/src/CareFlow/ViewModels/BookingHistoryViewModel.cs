using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CareFlow.Models;
using CareFlow.Services;

namespace CareFlow.ViewModels;

public partial class BookingHistoryViewModel : ViewModelBase
{
    private readonly IBookingService _bookingService;

    [ObservableProperty]
    private ObservableCollection<Booking> _bookings = new();

    [ObservableProperty]
    private Booking? _selectedBooking;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public BookingHistoryViewModel(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadBookingsAsync();
    }

    [RelayCommand]
    public async Task LoadBookingsAsync()
    {
        try
        {
            IsBusy = true;
            var bookingsList = await _bookingService.GetAllBookingsAsync();

            Bookings.Clear();
            foreach (var b in bookingsList)
            {
                Bookings.Add(b);
            }

            if (SelectedBooking == null && Bookings.Count > 0)
            {
                SelectedBooking = Bookings[0];
            }

            StatusMessage = $"Retrieved {Bookings.Count} confirmed booking(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error retrieving bookings: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
