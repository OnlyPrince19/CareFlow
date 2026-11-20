namespace CareFlow.Models;

public class DiagnosticService
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int DailyCapacity { get; set; }
    public string Description { get; set; } = string.Empty;

    public ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();
}
