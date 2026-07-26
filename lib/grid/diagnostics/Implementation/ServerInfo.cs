namespace Grid.Diagnostics;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemInformation;

using Logging;

/// <summary>
/// Model for server info.
/// </summary>
public class ServerInfo : IServerInfo
{
    private const float _KiloBytesInGigabyte = 1048576;
    private const float _BytesInGigabyte = 1073741824;
    private const int _TimerIntervalInMilliseconds = 2000;

    /// <summary>
    /// The logical core count for the host machine.
    /// </summary>
    public int LogicalCoreCount { get; set; }

    /// <summary>
    /// The physical core count for the host machine.
    /// </summary>
    public int PhysicalCoreCount { get; set; }

    /// <summary>
    /// The physical memory in GiB.
    /// </summary>
    public float TotalPhysicalMemoryInGigabytes { get; set; }

    /// <summary>
    /// The assembly version.
    /// </summary>
    public string AssemblyVersion { get; set; }

    /// <summary>
    /// The kernel version.
    /// </summary>
    public string KernelVersion { get; set; }

    /// <summary>
    /// Gets a static instance of <see cref="ServerInfo"/>
    /// </summary>
    /// <returns>The <see cref="ServerInfo"/></returns>
    public static ServerInfo GetInstance()
    {
        return new ServerInfo
        {
            AssemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString(),
            LogicalCoreCount = Environment.ProcessorCount,
            PhysicalCoreCount = GetPhysicalCoreCount(),
            TotalPhysicalMemoryInGigabytes = GetTotalPhysicalMemoryInGigabytes(),
            KernelVersion = GetKernelVersion()
        };
    }

    /// <summary>
    /// Gets the Kernel version depending on <see cref="OSPlatform"/>
    /// </summary>
    /// <param name="logger">An optional logger.</param>
    /// <returns>The Kernel Version</returns>
    public static string GetKernelVersion(ILogger logger = null)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return Environment.OSVersion.Version.ToString();

            return File.ReadAllText("/proc/version").Split(' ')[2];
        }
        catch (Exception ex)
        {
            logger?.Error(ex);

            return null;
        }
    }

    /// <summary>
    /// Gets the total physical memory in GB depending on <see cref="OSPlatform"/>
    /// </summary>
    /// <param name="logger">An optional logger.</param>
    /// <returns>The total physical memory.</returns>
    public static float GetTotalPhysicalMemoryInGigabytes(ILogger logger = null) => ExtractMemInfoValue("MemTotal", logger);

    /// <summary>
    /// Gets the available physical memory in GB depending on <see cref="OSPlatform"/>
    /// </summary>
    /// <param name="logger">An optional logger.</param>
    /// <returns>The available physical memory.</returns>
    public static float GetAvailablePhysicalMemoryInGigabytes(ILogger logger = null) => ExtractMemInfoValue("MemAvailable", logger);

    /// <summary>
    /// Gets the physical core count depending on the <see cref="OSPlatform"/>
    /// </summary>
    /// <param name="logger">An <see cref="ILogger"/> to pass in.</param>
    /// <returns>The physical core count of the server.</returns>
    public static int GetPhysicalCoreCount(ILogger logger = null)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return GetPhysicalCoreCountWindows(logger);

        return GetPhysicalCoreCountLinux(logger);
    }

    private static float ExtractMemInfoValue(string metric, ILogger logger)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return ExtractMemInfoValueWindows(metric, logger);

        return ExtractMemInfoValueLinux(metric, logger);
    }

    private static unsafe float ExtractMemInfoValueWindows(string metric, ILogger logger)
    {
        try
        {
            MEMORYSTATUSEX lpState = new();

            lpState.dwLength = (uint)Marshal.SizeOf(lpState);

            if (!PInvoke.GlobalMemoryStatusEx(&lpState))
                throw new InvalidOperationException($"Error calling to Kernel32::GlobalMemoryStatusEx: {(WIN32_ERROR)Marshal.GetLastWin32Error()}");

            var memInfo = metric switch
            {
                "MemTotal" => lpState.ullTotalPhys,
                "MemAvailable" => lpState.ullAvailPhys,
                _ => (ulong)0,
            };

            return memInfo / _BytesInGigabyte;
        }
        catch (Exception ex)
        {
            logger?.Error(ex);

            return 0;
        }
    }

    private static float ExtractMemInfoValueLinux(string metric, ILogger logger)
    {
        try
        {
            float.TryParse(
                new string(
                    (File.ReadAllLines("/proc/meminfo")
                    .FirstOrDefault(line => line.StartsWith(metric)) ?? string.Empty)
                    .Where(char.IsDigit).ToArray()
                ),
                out var memInfo
            );

            return memInfo / _KiloBytesInGigabyte;
        }
        catch (Exception ex)
        {
            logger?.Error(ex);

            return 0;
        }
    }

    private static unsafe int GetPhysicalCoreCountWindows(ILogger logger)
    {
        uint bufferLength = 0;
        PInvoke.GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationAll, null, ref bufferLength);

        var buffer = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)Marshal.AllocHGlobal((int)bufferLength);

        static int _CountBitsSet(ulong value)
        {
            int count = 0;
            while (value != 0)
            {
                count += (int)(value & 1);
                value >>= 1;
            }
            return count;
        }

        try
        {
            if (!PInvoke.GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationAll, buffer, ref bufferLength))
            {
                logger?.Error("Error calling to Kernel32::GetSystemTimes: {0}", (WIN32_ERROR)Marshal.GetLastWin32Error());

                return 0;
            }

            int physicalCoreCount = 0;
            int logicalCoreCount = 0;

            byte* ptr = (byte*)buffer;
            byte* endPtr = ptr + bufferLength;

            while (ptr < endPtr)
            {
                var information = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)ptr;

                if (information->Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                {
                    physicalCoreCount++;
                    for (int i = 0; i < information->Anonymous.Processor.GroupCount; i++)
                        logicalCoreCount += _CountBitsSet(information->Anonymous.Processor.GroupMask[i].Mask);
                }

                ptr += information->Size;
            }

            return physicalCoreCount * logicalCoreCount;
        }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return 0;
        }
        finally
        {
            if (buffer != null)
                Marshal.FreeHGlobal((IntPtr)buffer);
        }
    }

    private static int GetPhysicalCoreCountLinux(ILogger logger)
    {
        try
        {
            var cpuInfo = File.ReadAllLines("/proc/cpuinfo");
            var currentLine = cpuInfo.FirstOrDefault(line => line.StartsWith("cpu cores"));
            if (currentLine == null)
            {
                logger?.Error("Unable to find cpu cores line(s) in /proc/cpuinfo");
                return 0;
            }

            var info = (from s in currentLine.Split(':')
                        select s.Trim()).ToArray();

            if (info.Length != 2)
            {
                logger?.Error("Unable to parse 'cpu cores' line: {0}", currentLine);
                return 0;
            }

            var firstInfo = info[1];
            if (!int.TryParse(firstInfo, out int num))
            {
                logger?.Error("Unable to parse 'cpu cores' value: {0}", firstInfo);
                return 0;
            }

            return (from line in cpuInfo
                    where line.StartsWith("physical id")
                    select line).Distinct().Count() * num;
        }
        catch (Exception ex)
        {
            logger?.Error(ex);
            return 0;
        }
    }
}