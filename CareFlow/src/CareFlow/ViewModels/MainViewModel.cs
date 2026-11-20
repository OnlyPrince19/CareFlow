using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CareFlow.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private NewBookingViewModel _newBooking;

    [ObservableProperty]
    private AvailabilityViewModel _availability;

    [ObservableProperty]
    private BookingHistoryViewModel _history;

    [ObservableProperty]
    private int _selectedTabIndex;

    public MainViewModel(
        NewBookingViewModel newBooking,
        AvailabilityViewModel availability,
        BookingHistoryViewModel history)
    {
        _newBooking = newBooking;
        _availability = availability;
        _history = history;

        NewBooking.BookingConfirmed += OnBookingConfirmed;
    }

    [RelayCommand]
    public async Task InitializeAllAsync()
    {
        await NewBooking.InitializeAsync();
        await Availability.InitializeAsync();
        await History.InitializeAsync();
    }

    private void OnBookingConfirmed()
    {
        _ = Availability.LoadAvailabilityAsync();
        _ = History.LoadBookingsAsync();
    }
}
