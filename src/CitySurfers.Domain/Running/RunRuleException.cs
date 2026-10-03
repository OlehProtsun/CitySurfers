namespace CitySurfers.Domain.Running;

public enum RunError { Validation, Conflict, NotFound }

public sealed class RunRuleException(RunError error, string message) : Exception(message)
{
    public RunError Error { get; } = error;
}
