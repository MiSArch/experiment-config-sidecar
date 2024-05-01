namespace ExperimentConfigSidecar.Models
{
    /// <summary>
    /// Represents variable definitions.
    /// </summary>
    /// <param name="Configuration">All variable definitions</param>
    public record VariableDefinitions(Dictionary<string, VariableDefinition> Configuration);
}