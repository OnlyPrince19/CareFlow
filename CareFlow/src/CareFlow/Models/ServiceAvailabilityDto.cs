namespace CareFlow.Models;

public class ServiceAvailabilityDto
{
    public int ServiceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int DailyCapacity { get; set; }
    public int BookedCount { get; set; }
    public int RemainingCapacity => Math.Max(0, DailyCapacity - BookedCount);
    public bool IsAvailable => RemainingCapacity > 0;
}
