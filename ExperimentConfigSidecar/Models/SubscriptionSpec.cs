namespace ExperimentConfigSidecar.Models;

/// <summary>
/// Dapr subscription configuration.
/// </summary>
public class SubscriptionSpec
{
    /// <summary>
    /// The topic to subscribe to.
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// The pubsub name.
    /// </summary>
    public string Pubsubname { get; set; }

    /// <summary>
    /// The metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// The route.
    /// </summary>
    public string Route { get; set; }

    /// <summary>
    /// Bulk subscription configuration.
    /// </summary>
    public BulkSubscribe BulkSubscribe { get; set; }

    /// <summary>
    /// The max number of retries.
    /// </summary>
    public string? DeadLetterTopic { get; set; }
}

/// <summary>
/// Dapr bulk subscription configuration.
/// </summary>
public class BulkSubscribe
{
    /// <summary>
    /// If true, do a bulk subscription, otherwise do a regular subscription.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The max number of messages to process.
    /// </summary>
    public int? MaxMessagesCount { get; set; }

    /// <summary>
    /// The max duration to wait for messages.
    /// </summary>
    public int? MaxAwaitDurationMs { get; set; }
}