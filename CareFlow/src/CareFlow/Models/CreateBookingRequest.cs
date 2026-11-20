namespace CareFlow.Models;

public class CreateBookingRequest
{
    public int PatientId { get; set; }
    public DateOnly BookingDate { get; set; }
    public List<int> SelectedServiceIds { get; set; } = new();
    public string? Notes { get; set; }
    
    /// <summary>
    /// For verifying Acceptance Scenario AC-05:
    /// Forces an intentional exception before committing the transaction to verify
    /// that no partial booking or capacity changes persist.
    /// </summary>
    public bool SimulateFailureBeforeCommit { get; set; } = false;
}
