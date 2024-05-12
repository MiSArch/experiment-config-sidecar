using System.Text.Json;

namespace ExperimentConfigSidecar.Models;

/// <summary>
/// Represents a variable definition.
/// </summary>
/// <param name="Type">The type of the variable.</param>
/// <param name="DefaultValue">The default value of the variable.</param>
public record VariableDefinition(JsonElement Type, JsonElement DefaultValue);