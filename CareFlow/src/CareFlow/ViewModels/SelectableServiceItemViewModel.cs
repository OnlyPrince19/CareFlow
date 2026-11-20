using CommunityToolkit.Mvvm.ComponentModel;
using CareFlow.Models;

namespace CareFlow.ViewModels;

public partial class SelectableServiceItemViewModel : ViewModelBase
{
    public int ServiceId { get; }
    public string Code { get; }
    public string Name { get; }
    public string Department { get; }
    public int DailyCapacity { get; }

    [ObservableProperty]
    private int _bookedCount;

    [ObservableProperty]
    private bool _isSelected;

    public int RemainingCapacity => Math.Max(0, DailyCapacity - BookedCount);
    public bool IsAvailable => RemainingCapacity > 0;
    public string CapacityDisplay => $"{RemainingCapacity} / {DailyCapacity} left";

    public SelectableServiceItemViewModel(ServiceAvailabilityDto dto)
    {
        ServiceId = dto.ServiceId;
        Code = dto.Code;
        Name = dto.Name;
        Department = dto.Department;
        DailyCapacity = dto.DailyCapacity;
        _bookedCount = dto.BookedCount;
    }

    public void UpdateAvailability(int bookedCount)
    {
        BookedCount = bookedCount;
        OnPropertyChanged(nameof(RemainingCapacity));
        OnPropertyChanged(nameof(IsAvailable));
        OnPropertyChanged(nameof(CapacityDisplay));
    }
}
