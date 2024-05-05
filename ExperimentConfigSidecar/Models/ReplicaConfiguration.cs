using System.Text.Json;

namespace ExperimentConfigSidecar.Models;

/// <summary>
/// Configuration for a single replica
/// </summary>
public class ReplicaConfiguration
{
    /// <summary>
    /// The ID of the replica that triggered the event.
    /// </summary>
    public string ReplicaId { get; set; }

    /// <summary>
    /// The new variables.
    /// </summary>
    public Dictionary<string, JsonElement> Variables { get; set; }
}