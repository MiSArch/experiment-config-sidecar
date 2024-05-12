namespace ExperimentConfigSidecar.Models;

/// <summary>
/// Represents the Pubsub deterioration rule configuration.
/// </summary>
/// <param name="Delay">The delay in milliseconds to introduce in the Pubsub message processing.</param>
/// <param name="DelayProbability">The probability of introducing a delay.</param>
/// <param name="ErrorProbability">The probability of introducing an error.</param>
public record PubsubDetertiorationRule(int Delay, double? DelayProbability, double? ErrorProbability);