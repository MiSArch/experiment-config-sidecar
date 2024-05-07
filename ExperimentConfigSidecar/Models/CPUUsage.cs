namespace ExperimentConfigSidecar.Models;

/// <summary>
/// Represents the CPU usage configuration.
/// A CPU usage works like this:
/// <code>
/// while (true)
/// {
///     cycle the cpu for UsageDuration milliseconds
///     pause for PauseDuration milliseconds
/// }
/// </code>
/// </summary>
/// <param name="UsageDuration">The duration in milliseconds to use CPU (keep a thread spinning).</param>
/// <param name="PauseDuration">The duration in milliseconds to pause between CPU usage.</param>
public record CPUUsage(int UsageDuration, int PauseDuration);