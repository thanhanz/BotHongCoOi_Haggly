namespace Haggly.Infrastructure.Payments;

public sealed class SimulatedPaymentOptions
{
    public const string SectionName = "Payments:Simulator";

    public SimulatedPaymentOutcome Outcome { get; init; } = SimulatedPaymentOutcome.Success;
    public string FailureReason { get; init; } = "Simulated provider decline.";

    public bool IsValid()
        => Enum.IsDefined(Outcome)
           && (Outcome != SimulatedPaymentOutcome.Failure
               || !string.IsNullOrWhiteSpace(FailureReason));
}

public enum SimulatedPaymentOutcome
{
    Success,
    Failure
}
