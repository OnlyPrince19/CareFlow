namespace CareFlow.Exceptions;

public class CareFlowValidationException : Exception
{
    public string RuleId { get; }

    public CareFlowValidationException(string ruleId, string message) : base(message)
    {
        RuleId = ruleId;
    }
}
