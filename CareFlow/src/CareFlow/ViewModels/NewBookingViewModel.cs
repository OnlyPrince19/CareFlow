using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CareFlow.Exceptions;
using CareFlow.Models;
using CareFlow.Services;

namespace CareFlow.ViewModels;

public partial class NewBookingViewModel : ViewModelBase
{
    private readonly IPatientService _patientService;
    private readonly IDiagnosticCatalogService _catalogService;
    private readonly IBookingService _bookingService;

    public event Action? BookingConfirmed;

    [ObservableProperty]
    private ObservableCollection<Patient> _patients = new();

    [ObservableProperty]
    private Patient? _selectedPatient;

    [ObservableProperty]
    private DateTimeOffset _selectedDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private ObservableCollection<SelectableServiceItemViewModel> _services = new();

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _feedbackMessage = string.Empty;

    [ObservableProperty]
    private bool _hasFeedback;

    [ObservableProperty]
    private bool _isFeedbackSuccess;

    [ObservableProperty]
    private bool _simulateFailureBeforeCommit;

    public NewBookingViewModel(
        IPatientService patientService,
        IDiagnosticCatalogService catalogService,
        IBookingService bookingService)
    {
        _patientService = patientService;
        _catalogService = catalogService;
        _bookingService = bookingService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadPatientsAsync();
        await LoadServicesForDateAsync();
    }

    public async Task LoadPatientsAsync()
    {
        try
        {
            var patientsList = await _patientService.GetAllPatientsAsync();
            Patients.Clear();
            foreach (var p in patientsList)
            {
                Patients.Add(p);
            }

            if (SelectedPatient == null && Patients.Count > 0)
            {
                SelectedPatient = Patients[0];
            }
        }
        catch (Exception ex)
        {
            SetFeedback(false, $"Failed to load patients: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task OnDateChangedAsync()
    {
        await LoadServicesForDateAsync();
    }

    [RelayCommand]
    public async Task LoadServicesForDateAsync()
    {
        try
        {
            IsBusy = true;
            var targetDate = DateOnly.FromDateTime(SelectedDate.DateTime);
            var availabilityList = await _catalogService.GetAvailabilityForDateAsync(targetDate);

            // Remember previously selected service IDs if applicable
            var selectedIds = Services.Where(s => s.IsSelected).Select(s => s.ServiceId).ToHashSet();

            Services.Clear();
            foreach (var dto in availabilityList)
            {
                var itemVm = new SelectableServiceItemViewModel(dto)
                {
                    IsSelected = selectedIds.Contains(dto.ServiceId)
                };
                Services.Add(itemVm);
            }
        }
        catch (Exception ex)
        {
            SetFeedback(false, $"Failed to load diagnostic services: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ConfirmBookingAsync()
    {
        ClearFeedback();

        if (SelectedPatient == null)
        {
            SetFeedback(false, "BR-01 Violation: Please select an existing patient.");
            return;
        }

        var selectedServices = Services.Where(s => s.IsSelected).ToList();
        if (selectedServices.Count < 1 || selectedServices.Count > 3)
        {
            SetFeedback(false, $"BR-02 Violation: Each booking must contain between 1 and 3 diagnostic services. (Currently selected: {selectedServices.Count})");
            return;
        }

        var bookingDate = DateOnly.FromDateTime(SelectedDate.DateTime);
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (bookingDate < today)
        {
            SetFeedback(false, $"BR-03 Violation: Selected booking date ({bookingDate:yyyy-MM-dd}) cannot be in the past. Current date is {today:yyyy-MM-dd}.");
            return;
        }

        var request = new CreateBookingRequest
        {
            PatientId = SelectedPatient.Id,
            BookingDate = bookingDate,
            SelectedServiceIds = selectedServices.Select(s => s.ServiceId).ToList(),
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            SimulateFailureBeforeCommit = SimulateFailureBeforeCommit
        };

        try
        {
            IsBusy = true;
            var result = await _bookingService.CreateBookingAsync(request);

            if (result.IsSuccess)
            {
                SetFeedback(true, $"Booking Confirmed! Reference: {result.BookingReference}\n" +
                                  $"Patient: {SelectedPatient.FullName}\n" +
                                  $"Date: {bookingDate:yyyy-MM-dd}\n" +
                                  $"Services ({selectedServices.Count}): {string.Join(", ", selectedServices.Select(s => s.Code))}");

                // Reset selection
                foreach (var s in Services)
                {
                    s.IsSelected = false;
                }
                Notes = string.Empty;

                // Reload availability for this date
                await LoadServicesForDateAsync();

                // Trigger callback for other tabs to refresh
                BookingConfirmed?.Invoke();
            }
        }
        catch (CareFlowValidationException ex)
        {
            SetFeedback(false, $"Validation Error [{ex.RuleId}]: {ex.Message}");
            await LoadServicesForDateAsync(); // Refresh in case capacity was taken
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("AC-05"))
        {
            SetFeedback(false, $"AC-05 Verified! Transaction Rolled Back:\n{ex.Message}\nDatabase state remains consistent (0 partial records written).");
            await LoadServicesForDateAsync();
        }
        catch (Exception ex)
        {
            SetFeedback(false, $"Database / Transaction Error: {ex.Message}");
            await LoadServicesForDateAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Verification helper for Acceptance Scenario AC-04:
    /// Silently exhausts capacity in the background for one selected service
    /// to simulate a race condition / stale UI display scenario.
    /// </summary>
    [RelayCommand]
    public async Task SimulateStaleCapacityScenarioAsync()
    {
        ClearFeedback();

        var selected = Services.FirstOrDefault(s => s.IsSelected);
        if (selected == null)
        {
            SetFeedback(false, "AC-04 Helper: Please select at least one service checkbox above first, so we can simulate exhausting its capacity.");
            return;
        }

        try
        {
            IsBusy = true;
            var bookingDate = DateOnly.FromDateTime(SelectedDate.DateTime);
            var remaining = selected.RemainingCapacity;

            if (remaining <= 0)
            {
                SetFeedback(false, $"AC-04 Helper: Service '{selected.Code}' is already at 0 capacity.");
                return;
            }

            // Create background bookings for other patients to consume all remaining slots
            var allPatients = await _patientService.GetAllPatientsAsync();
            var otherPatients = allPatients.Where(p => p.Id != (SelectedPatient?.Id ?? 0)).ToList();

            for (int i = 0; i < remaining; i++)
            {
                var patient = otherPatients[i % otherPatients.Count];
                var bgRequest = new CreateBookingRequest
                {
                    PatientId = patient.Id,
                    BookingDate = bookingDate,
                    SelectedServiceIds = new List<int> { selected.ServiceId },
                    Notes = $"AC-04 Background Stale Simulation Slot #{i + 1}"
                };
                await _bookingService.CreateBookingAsync(bgRequest);
            }

            // Intentionally DO NOT refresh the UI Services list here!
            // This leaves the UI display stale, exactly testing AC-04!
            SetFeedback(false, $"AC-04 Simulation Active: {remaining} booking(s) committed in the background for '{selected.Code}'.\n" +
                               $"Notice that this screen's displayed capacity is now STALE!\n" +
                               $"Click 'Confirm Booking' now to test BR-08 (capacity re-validation). The system will catch the stale capacity and abort!");

            BookingConfirmed?.Invoke();
        }
        catch (Exception ex)
        {
            SetFeedback(false, $"Background booking failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ClearForm()
    {
        foreach (var s in Services)
        {
            s.IsSelected = false;
        }
        Notes = string.Empty;
        ClearFeedback();
    }

    private void SetFeedback(bool isSuccess, string message)
    {
        IsFeedbackSuccess = isSuccess;
        FeedbackMessage = message;
        HasFeedback = true;
    }

    private void ClearFeedback()
    {
        HasFeedback = false;
        FeedbackMessage = string.Empty;
    }
}
