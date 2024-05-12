namespace ExperimentConfigSidecar.Models;

/// <summary>
/// Represents a deterioration of either a pubsub (async event) or service invocation (http call) from dapr.
/// </summary>
/// <param name="Delay">The delay in milliseconds to introduce.</param>
/// <param name="ErrorCode">If present, the error code to return.</param>
public record Deterioration(int? Delay, int? ErrorCode);