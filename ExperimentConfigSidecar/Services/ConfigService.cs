using System.Text.Json;
using ExperimentConfigSidecar.Models;

namespace ExperimentConfigSidecar.Services;

/// <summary>
/// Service to parse, manage and apply configuration properties.
/// Also manages artificial memory and CPU usage.
/// </summary>
public class ConfigService
{

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
    private const string PubsubDeteriorationSchema = """
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
    private const string ServiceInvocationDeteriorationSchema = """
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
    private const string MemoryUsageSchema = """
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
    private const string CPUUsageSchema = """
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
    private readonly HashSet<string> configPropertyKeys = new([PubsubDeteriorationKey, ServiceInvocationDeteriorationKey, MemoryUsageKey, CPUUsageKey]);

    /// <summary>
    /// List of current service invocation deterioration rules.
    /// </summary>
    private List<ServiceInvocationDeteriorationRule> serviceInvocationDeteriorationRules = [];

    /// <summary>
    /// Current pubsub deterioration rule.
    /// </summary>
    private PubsubDetertiorationRule pubsubDetertiorationRule = new(0, null, null);

    /// <summary>
    /// Current artificial memory usage in bytes.
    /// </summary>
    private long artificialMemoryUsage = 0;

    /// <summary>
    /// Current artificial CPU usages.
    /// </summary>
    private List<CPUUsage> artificialCPUUsage = [];

    /// <summary>
    /// Random number generator used by this service.
    /// </summary>
    private readonly Random random = new();

    /// <summary>
    /// Service to manage artificial memory usage.
    /// </summary>
    private readonly MemoryUsageService memoryUsageService = new();

    /// <summary>
    /// Service to manage artificial CPU usage.
    /// </summary>
    private readonly CPUUsageService cpuUsageService = new();

    /// <summary>
    /// Update the configuration properties handled by the sidecar itself.
    /// Returns the remaining configuration properties.
    /// </summary>
    /// <param name="config">All (unparsed) configuration properties</param>
    /// <returns>The remaining configuration propeties which need to be passed to the service</returns>
    public Dictionary<string, JsonElement> UpdateConfig(Dictionary<string, JsonElement> config)
    {
        UpdatePubsubDeterioration(config);
        UpdateServiceInvocationDeterioration(config);
        UpdateArtificialMemoryUsage(config);
        UpdateArtificialCPUUsage(config);
        return config.Where(pair => !configPropertyKeys.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    /// <summary>
    /// Update the artificial memory usage based on the provided config.
    /// </summary>
    /// <param name="config">Contains all config properties</param>
    private void UpdateArtificialMemoryUsage(Dictionary<string, JsonElement> config)
    {
        if (config.TryGetValue(MemoryUsageKey, out JsonElement value))
        {
            artificialMemoryUsage = value.TryGetInt64(out var leak) ? leak : 0;
        }
        memoryUsageService.UpdateMemoryUsage(artificialMemoryUsage);
    }

    /// <summary>
    /// Update the artificial CPU usage based on the provided config.
    /// </summary>
    /// <param name="config">Contains all config properties</param>
    private void UpdateArtificialCPUUsage(Dictionary<string, JsonElement> config)
    {
        artificialCPUUsage = [];
        if (config.TryGetValue(CPUUsageKey, out JsonElement value) && !value.IsNull())
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                artificialCPUUsage.Add(ParseCPUUsage(value));
            }
            else
            {
                foreach (var usage in value.EnumerateArray())
                {
                    artificialCPUUsage.Add(ParseCPUUsage(usage));
                }
            }
        }
        cpuUsageService.UpdateCPUUsage(artificialCPUUsage);
    }

    /// <summary>
    /// Update the service invocation deterioration rules based on the provided config.
    /// </summary>
    /// <param name="config">Contains all config properties</param>
    private void UpdateServiceInvocationDeterioration(Dictionary<string, JsonElement> config)
    {
        serviceInvocationDeteriorationRules = [];
        if (config.TryGetValue(ServiceInvocationDeteriorationKey, out JsonElement value) && !value.IsNull())
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                serviceInvocationDeteriorationRules.Add(ParseServiceInvocationDeteriorationRule(value));
            }
            else
            {
                foreach (var rule in value.EnumerateArray())
                {
                    serviceInvocationDeteriorationRules.Add(ParseServiceInvocationDeteriorationRule(rule));
                }
            }
        }
    }

    /// <summary>
    /// Update the pubsub deterioration rule based on the provided config.
    /// </summary>
    /// <param name="config">Contains all config properties</param>
    private void UpdatePubsubDeterioration(Dictionary<string, JsonElement> config)
    {
        if (config.TryGetValue(PubsubDeteriorationKey, out JsonElement value) && !value.IsNull())
        {
            pubsubDetertiorationRule = new
            (
                value.GetIntProperty("delay") ?? 0,
                value.GetDoubleProperty("delayProbability"),
                value.GetDoubleProperty("errorProbability")
            );
        }
        else
        {
            pubsubDetertiorationRule = new(0, null, null);
        }
    }

    /// <summary>
    /// Parse a service invocation deterioration rule from a JSON object.
    /// </summary>
    /// <param name="rule">The rule to parse</param>
    /// <returns>The parsed rule</returns>
    private ServiceInvocationDeteriorationRule ParseServiceInvocationDeteriorationRule(JsonElement rule)
    {
        return new ServiceInvocationDeteriorationRule
        (
            rule.GetStringProperty("path"),
            rule.GetDoubleProperty("delayProbability"),
            rule.GetIntProperty("delay") ?? 0,
            rule.GetDoubleProperty("errorProbability"),
            rule.GetIntProperty("errorCode") ?? 500
        );
    }

    /// <summary>
    /// Parse a CPU usage from a JSON object.
    /// </summary>
    /// <param name="usage">The CPU usage to parse</param>
    /// <returns>The parsed CPU usage</returns>
    private CPUUsage ParseCPUUsage(JsonElement usage)
    {
        return new CPUUsage
        (
            usage.GetIntProperty("usageDuration") ?? 0,
            usage.GetIntProperty("pauseDuration") ?? 0
        );
    }

    /// <summary>
    /// Get a pubsub deterioration which can be applied to a pubsub call from the dapr sidecar.
    /// Decides based on the current configuration weather to delay the call and/or return an error.
    /// </summary>
    /// <returns>The deterioration for a pubsub call</returns>
    public Deterioration GetPubsubDeterioration()
    {
        return new Deterioration
        (
            random.NextDouble() < pubsubDetertiorationRule.DelayProbability ? pubsubDetertiorationRule.Delay : null,
            random.NextDouble() < pubsubDetertiorationRule.ErrorProbability ? 500 : null
        );

    }

    /// <summary>
    /// Get a service invocation deterioration which can be applied to a service call from the dapr sidecar.
    /// Decides based on the current configuration and the path weather to delay the call and/or return an error.
    /// </summary>
    /// <param name="path">Request path, used to find applicable rule</param>
    /// <returns>The deterioration for a service invocation call</returns>
    public Deterioration GetServiceInvocationDeterioration(string path)
    {
        foreach (var rule in serviceInvocationDeteriorationRules)
        {
            if (rule.Path == null || path.StartsWith(rule.Path))
            {
                return new Deterioration
                (
                    random.NextDouble() < rule.DelayProbability ? rule.Delay : null,
                    random.NextDouble() < rule.ErrorProbability ? rule.ErrorCode : null
                );
            }
        }
        return new Deterioration(null, null);
    }

    /// <summary>
    /// Add the configuration properties handled by the sidecar itself to the provided dictionary.
    /// Overwrites existing definitions if necessary.
    /// </summary>
    /// <param name="existingDefinitions">Existing variable definitions from the service</param>
    public void AddVariableDefinitions(Dictionary<string, VariableDefinition> existingDefinitions)
    {
        existingDefinitions.Add(PubsubDeteriorationKey, new VariableDefinition(PubsubDeteriorationSchema.AsJsonElement(), "null".AsJsonElement()));
        existingDefinitions.Add(ServiceInvocationDeteriorationKey, new VariableDefinition(ServiceInvocationDeteriorationSchema.AsJsonElement(), "null".AsJsonElement()));
        existingDefinitions.Add(MemoryUsageKey, new VariableDefinition(MemoryUsageSchema.AsJsonElement(), "null".AsJsonElement()));
        existingDefinitions.Add(CPUUsageKey, new VariableDefinition(CPUUsageSchema.AsJsonElement(), "null".AsJsonElement()));
    }
}

