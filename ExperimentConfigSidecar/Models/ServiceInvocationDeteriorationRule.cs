namespace ExperimentConfigSidecar.Models;

/// <summary>
/// Represents the service invocation deterioration configuration.
/// </summary>
/// <param name="Path">The path prefix to match the service invocation.</param>
/// <param name="DelayProbability">The probability of introducing a delay.</param>
/// <param name="Delay">The delay in milliseconds to introduce in the service invocation.</param>
/// <param name="ErrorProbability">The probability of introducing an error.</param>
/// <param name="ErrorCode">The error code to return in the service invocation.</param>
public record ServiceInvocationDeteriorationRule(string? Path, double? DelayProbability, int? Delay, double? ErrorProbability, int ErrorCode);