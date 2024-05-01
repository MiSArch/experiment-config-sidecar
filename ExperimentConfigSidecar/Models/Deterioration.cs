namespace ExperimentConfigSidecar.Models;

/// <summary>
/// Represents a deterioration of either a pubsub or service invocation.
/// </summary>
/// <param name="Delay">The delay in milliseconds to introduce.</param>
/// <param name="ErrorCode">If present, the error code to return.</param>
public record Deterioration(int? Delay, int? ErrorCode);