namespace ExperimentConfigSidecar.Models;

/// <summary>
/// Event from the experiment config service to signal a configuration change.
/// </summary>
public class ConfigurationEvent
{
    /// <summary>
    /// The configurations that have changed.
    /// </summary>
    public List<ReplicaConfiguration> Configurations { get; set; }

}