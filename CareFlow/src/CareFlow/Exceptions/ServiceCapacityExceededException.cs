namespace CareFlow.Exceptions;

public class ServiceCapacityExceededException : CareFlowValidationException
{
    public int ServiceId { get; }
    public string ServiceName { get; }
    public DateOnly TargetDate { get; }

    public ServiceCapacityExceededException(int serviceId, string serviceName, DateOnly targetDate)
        : base("BR-05", $"Diagnostic service '{serviceName}' has already reached its maximum daily capacity for {targetDate:yyyy-MM-dd}. Confirmed bookings cannot exceed the service daily limit.")
    {
        ServiceId = serviceId;
        ServiceName = serviceName;
        TargetDate = targetDate;
    }
}
