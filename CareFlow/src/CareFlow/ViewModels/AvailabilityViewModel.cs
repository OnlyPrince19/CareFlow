using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CareFlow.Models;
using CareFlow.Services;

namespace CareFlow.ViewModels;

public partial class AvailabilityViewModel : ViewModelBase
{
    private readonly IDiagnosticCatalogService _catalogService;

    [ObservableProperty]
    private DateTimeOffset _selectedDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private ObservableCollection<ServiceAvailabilityDto> _services = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public AvailabilityViewModel(IDiagnosticCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadAvailabilityAsync();
    }

    [RelayCommand]
    public async Task OnDateChangedAsync()
    {
        await LoadAvailabilityAsync();
    }

    [RelayCommand]
    public async Task LoadAvailabilityAsync()
    {
        try
        {
            IsBusy = true;
            var targetDate = DateOnly.FromDateTime(SelectedDate.DateTime);
            var availability = await _catalogService.GetAvailabilityForDateAsync(targetDate);

            Services.Clear();
            foreach (var item in availability)
            {
                Services.Add(item);
            }

            StatusMessage = $"Availability updated for {targetDate:MMMM dd, yyyy}. Total diagnostic services: {Services.Count}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error retrieving availability: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
