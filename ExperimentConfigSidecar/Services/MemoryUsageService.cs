using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ExperimentConfigSidecar.Services;

/// <summary>
/// Service to manage artificial memory usage.
/// </summary>
public class MemoryUsageService
{

    /// <summary>
    /// The handle to the memory block.
    /// </summary>
    private IntPtr memoryHandle = IntPtr.Zero;

    /// <summary>
    /// The current memory usage in bytes.
    /// </summary>
    private long memoryUsage = 0;

    /// <summary>
    /// The object to lock on.
    /// </summary>
    private readonly object mutex = new();

    /// <summary>
    /// Updates the memory usage.
    /// If the memory usage changes, the memory block is reallocated and initialized.
    /// </summary>
    /// <param name="bytes">The new memory usage in bytes</param>
    public void UpdateMemoryUsage(long bytes)
    {
        lock (mutex)
        {
            if (memoryUsage != bytes)
            {
                memoryUsage = bytes;
                if (memoryHandle != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(memoryHandle);
                }
                memoryHandle = Marshal.AllocHGlobal((nint)bytes);
                InitMemory(bytes);
            }
        }
    }

    /// <summary>
    /// Initializes the memory block.
    /// Necessary because simply allocating memory does not guarantee that it is initialized.
    /// </summary>
    /// <param name="bytes">The number of bytes to initialize</param>
    private void InitMemory(long bytes)
    {
        unsafe
        {
            nint toInit = (nint)bytes;
            nint allocated = 0;
            while (toInit > 0)
            {
                uint toInitCurrently = (uint)Math.Min(toInit, uint.MaxValue);
                Unsafe.InitBlockUnaligned((void*)(memoryHandle + allocated), 1, toInitCurrently);
                toInit -= (nint)toInitCurrently;
                allocated += (nint)toInitCurrently;
            }
            Unsafe.InitBlockUnaligned((void*)memoryHandle, 1, (uint)bytes);
        }
    }
}