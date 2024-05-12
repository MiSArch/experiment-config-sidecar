namespace ExperimentConfigSidecar.Services;

/// <summary>
/// Definition of configuration properties handled by the sidecar itself.
/// Provides keys and JSON schemas for these properties.
/// </summary>
public static class ConfigPropertyDefinitions {
    /// <summary>
    /// Key for the pubsub deterioration configuration property.
    /// </summary>
    public const string PubsubDeteriorationKey = "pubsubDeterioration";

    /// <summary>
    /// Key for the service invocation deterioration configuration property.
    /// </summary>
    public const string ServiceInvocationDeteriorationKey = "serviceInvocationDeterioration";

    /// <summary>
    /// Key for the artificial memory usage configuration property.
    /// </summary>
    public const string MemoryUsageKey = "artificialMemoryUsage";

    /// <summary>
    /// Key for the artificial CPU usage configuration property.
    /// </summary>
    public const string CPUUsageKey = "artificialCPUUsage";

    /// <summary>
    /// JSON schema for the pubsub deterioration configuration property.
    /// </summary>
    public const string PubsubDeteriorationSchema = """
    {
        "$schema": "http://json-schema.org/draft-07/schema#",
        "oneOf": [
            {
                "type": "object",
                "properties": {
                    "delayProbability": {"type": "number"},
                    "errorProbability": {"type": "number"},
                    "delay": {"type": "integer"}
                },
                "additionalProperties": false
            },
            {
                "type": "null"
            }
        ]
    }
    """;

    /// <summary>
    /// JSON schema for the service invocation deterioration configuration property.
    /// </summary>
    public const string ServiceInvocationDeteriorationSchema = """
    {
        "$schema": "http://json-schema.org/draft-07/schema#",
        "$defs": {
            "item": {
                "type": "object",
                "properties": {
                    "path": {"type": "string"},
                    "delayProbability": {"type": "number"},
                    "delay": {"type": "integer"},
                    "errorProbability": {"type": "number"},
                    "errorCode": {"type": "integer"}
                },
                "additionalProperties": false
            }
        },
        "oneOf": [
            { "$ref": "#/$defs/item" },
            {
                "type": "array",
                "items": { "$ref": "#/$defs/item" }
            },
            {
                "type": "null"
            }
        ]
    }
    """;

    /// <summary>
    /// JSON schema for the artificial memory usage configuration property.
    /// </summary>
    public const string MemoryUsageSchema = """
    {
        "$schema": "http://json-schema.org/draft-07/schema#",
        "oneOf": [
            {
                "type": "integer"
            },
            {
                "type": "null"
            }
        ]
    }
    """;

    /// <summary>
    /// JSON schema for the artificial CPU usage configuration property.
    /// </summary>
    public const string CPUUsageSchema = """
    {
        "$schema": "http://json-schema.org/draft-07/schema#",
        "$defs": {
            "item": {
                "type": "object",
                "properties": {
                    "usageDuration": {"type": "integer"},
                    "pauseDuration": {"type": "integer"}
                },
                "required": ["usageDuration", "pauseDuration"],
                "additionalProperties": false
            }
        },
        "oneOf": [
            { "$ref": "#/$defs/item" },
            {
                "type": "array",
                "items": { "$ref": "#/$defs/item" }
            },
            {
                "type": "null"
            }
        ]
    }
    """;

    /// <summary>
    /// Set of keys for configuration properties handled by the sidecar itself.
    /// </summary>
    public static readonly HashSet<string> ConfigPropertyKeys = new([PubsubDeteriorationKey, ServiceInvocationDeteriorationKey, MemoryUsageKey, CPUUsageKey]);

}