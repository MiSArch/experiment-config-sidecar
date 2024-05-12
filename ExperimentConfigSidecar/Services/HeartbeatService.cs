using Dapr.Client;
using ExperimentConfigSidecar.Models;

namespace ExperimentConfigSidecar.Services;

/// <summary>
/// Service to send heartbeats to the experiment configuration sidecar.
/// </summary>
/// <param name="heartbeatInterval">The interval in milliseconds between heartbeats</param>
/// <param name="pubsubName">The name of the pubsub component</param>
/// <param name="replicaId">The unique identifier of the replica</param>
/// <param name="serviceName">The name of the service</param>
/// <param name="logger">The logger</param>
public class HeartbeatService(int heartbeatInterval, string pubsubName, Guid replicaId, string serviceName, ILogger logger)
{

    /// <summary>
    /// The Dapr client to use for publishing events.
    /// </summary>
    private readonly DaprClient daprClient = new DaprClientBuilder().Build();

    /// <summary>
    /// Starts sending heartbeats.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(heartbeatInterval);
                await daprClient.PublishEventAsync(pubsubName, "heartbeat", new HeartbeatEvent(replicaId, serviceName));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send heartbeat");
            }
        }
    }
}