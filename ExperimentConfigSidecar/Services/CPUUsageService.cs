using System.Diagnostics;
using ExperimentConfigSidecar.Models;

namespace ExperimentConfigSidecar.Services;

/// <summary>
/// Service to manage artificial CPU usage.
/// </summary>
public class CPUUsageService
{
    /// <summary>
    /// List of CPU usage handlers currently running.
    /// </summary>
    private readonly List<CPUUsageHandler> cpuUsageHandlers = [];

    /// <summary>
    /// Mutex to synchronize access to <see cref="cpuUsageHandlers"/>.
    /// </summary>
    private readonly object mutex = new();

    /// <summary>
    /// Updates the CPU usage.
    /// Stops / starts CPU usage handlers as needed and updates existing handlers.
    /// </summary>
    /// <param name="cpuUsages">The new CPU usage configuration</param>
    public void UpdateCPUUsage(List<CPUUsage> cpuUsages)
    {
        lock (mutex)
        {
            for (int i = cpuUsages.Count; i < cpuUsageHandlers.Count; i++)
            {
                cpuUsageHandlers[i].IsTerminated = true;
            }
            if (cpuUsageHandlers.Count > cpuUsages.Count)
            {
                cpuUsageHandlers.RemoveRange(cpuUsages.Count, cpuUsageHandlers.Count - cpuUsages.Count);
            }
            for (int i = 0; i < cpuUsageHandlers.Count; i++)
            {
                cpuUsageHandlers[i].PauseDuration = cpuUsages[i].PauseDuration;
                cpuUsageHandlers[i].UsageDuration = cpuUsages[i].UsageDuration;
            }
            for (int i = cpuUsageHandlers.Count; i < cpuUsages.Count; i++)
            {
                var usage = cpuUsages[i];
                var newHandler = new CPUUsageHandler(usage.UsageDuration, usage.PauseDuration);
                cpuUsageHandlers.Add(newHandler);
                new Thread(new ThreadStart(newHandler.Execute)).Start();
            }
        }
    }
}

class CPUUsageHandler
{
    /// <summary>
    /// The duration in milliseconds to use CPU (keep a thread spinning).
    /// </summary>
    private volatile int usageDuration;

    /// <summary>
    /// The duration in milliseconds to pause between CPU usage.
    /// </summary>
    private volatile int pauseDuration;

    /// <summary>
    /// Flag to indicate if the CPU usage handler has been terminated.
    /// If true, the handler will stop executing.
    /// </summary>
    private volatile bool isTerminated = false;

    /// <summary>
    /// The duration in milliseconds to use CPU (keep a thread spinning).
    /// </summary>
    public int UsageDuration
    {
        get => usageDuration;
        set => usageDuration = value;
    }

    /// <summary>
    /// The duration in milliseconds to pause between CPU usage.
    /// </summary>
    public int PauseDuration
    {
        get => pauseDuration;
        set => pauseDuration = value;
    }

    /// <summary>
    /// Flag to indicate if the CPU usage handler has been terminated.
    /// If true, the handler will stop executing.
    /// </summary>
    public bool IsTerminated
    {
        get => isTerminated;
        set => isTerminated = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CPUUsageHandler"/> class.
    /// </summary>
    /// <param name="usageDuration">The initial usage duration.</param>
    /// /// <param name="pauseDuration">The initial pause duration.</param>
    public CPUUsageHandler(int usageDuration, int pauseDuration)
    {
        UsageDuration = usageDuration;
        PauseDuration = pauseDuration;
    }

    /// <summary>
    /// Executes the CPU usage handler.
    /// Spins for <see cref="UsageDuration"/> milliseconds and then sleeps for <see cref="PauseDuration"/> milliseconds.
    /// Continues until <see cref="IsTerminated"/> is set to true.
    /// </summary>
    public void Execute()
    {
        var stopWatch = new Stopwatch();
        while (!IsTerminated)
        {
            var usageDuration = UsageDuration;
            stopWatch.Restart();
            while (stopWatch.ElapsedMilliseconds < usageDuration)
            {
                // spin
            }
            Thread.Sleep(PauseDuration);
        }
    }
};

