using System.Net.Sockets;

namespace ExperimentConfigSidecar.Services;

/// <summary>
/// Service to wait for the application (service) to start up.
/// Heavily inspired by the dapr sidecar logic.
/// </summary>
public class StartupService
{
    /// <summary>
    /// Waits for the application to start up.
    /// </summary>
    /// <param name="appPort">The port the application is listening on</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task WaitForStartup(int appPort)
    {
        while (true)
        {
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync("localhost", appPort);
                break;
            }
            catch
            {
                await Task.Delay(100);
            }
        }
    }
}