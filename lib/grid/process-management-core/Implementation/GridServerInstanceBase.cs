namespace Grid;

using System;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.ServiceModel;

using Logging;

using ComputeCloud;


/// <summary>
/// Represents the base class for a Grid Server Instance, implements <see cref="IGridServerInstance"/>
/// </summary>
public abstract class GridServerInstanceBase : IGridServerInstance
{
    private const string _LocalHost = "http://127.0.0.1:";
    private const int _TimeoutToQueryForVersion = 2000;

    private static readonly IPAddress _LocalHostIpAddress = IPAddress.Parse("127.0.0.1");

    /// <summary>
    /// The logger for this instance.
    /// </summary>
    protected readonly ILogger Logger;

    private readonly IJobManagerSettings _Settings;

    /// <inheritdoc cref="IGridServerInstance.Id"/>
    public abstract string Id { get; }

    /// <inheritdoc cref="IGridServerInstance.Name"/>
    public abstract string Name { get; }

    /// <inheritdoc cref="IGridServerInstance.HasExited"/>
    public abstract bool HasExited { get; }

    /// <inheritdoc cref="IGridServerInstance.MaximumCores"/>
    public double MaximumCores { get; set; }

    /// <inheritdoc cref="IGridServerInstance.MaximumThreads"/>
    public long MaximumThreads { get; set; }

    /// <inheritdoc cref="IGridServerInstance.MaximumMemoryInMegabytes"/>
    public long MaximumMemoryInMegabytes { get; set; }

    /// <inheritdoc cref="IGridServerInstance.Port"/>
    public int Port { get; }

    /// <inheritdoc cref="IGridServerInstance.Version"/>
    public string Version { get; }

    /// <inheritdoc cref="IGridServerInstance.ExpirationTime"/>
    public DateTime ExpirationTime { get; set; }

    /// <inheritdoc cref="IGridServerInstance.UseCount"/>
    public int UseCount { get; set; }

    /// <summary>
    /// Constructs a new instance of <see cref="GridServerInstanceBase"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="version">The version</param>
    /// <param name="port">The port</param>
    /// <param name="settings">The <see cref="IJobManagerSettings"/></param>
    protected GridServerInstanceBase(
        ILogger logger,
        string version,
        int port,
        IJobManagerSettings settings
    )
    {
        Logger = logger;
        Version = version;
        Port = port;
        _Settings = settings;
    }

    /// <summary>
    /// Constructs a new instance of <see cref="GridServerInstanceBase"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="port">The port</param>
    /// <param name="settings">The <see cref="IJobManagerSettings"/></param>
    protected GridServerInstanceBase(
        ILogger logger,
        int port,
        IJobManagerSettings settings
    )
    {
        Logger = logger;
        Port = port;
        Version = GetVersionFromGridServer();
        _Settings = settings;
    }

    /// <inheritdoc cref="IGridServerInstance.Start"/>
    public abstract bool Start();

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public abstract void Dispose();

    /// <inheritdoc cref="IGridServerInstance.GetSoapInterface(int)"/>
    public ComputeCloudServiceSoap GetSoapInterface(int timeoutInMilliseconds)
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            SendTimeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds)
        };

        return new ComputeCloudServiceSoapClient(binding, new EndpointAddress(_LocalHost + Port.ToString()));
    }

    /// <inheritdoc cref="IGridServerInstance.WaitForServiceToBecomeAvailable(bool, Stopwatch)"/>
    public void WaitForServiceToBecomeAvailable(bool forceTry, Stopwatch stopwatch)
    {
        var sw = Stopwatch.StartNew();
        var innerException = string.Empty;

        for (int attempt = 0; attempt < _Settings.GridServerStartAttempts; attempt++)
        {
            if (!forceTry && HasExited)
                throw new Exception(string.Format("Failed to connect to port {0} because GridServer container has already exited.", Port));

            using (var client = new TcpClient())
            {
                try
                {
                    client.Client.Connect(_LocalHostIpAddress, Port);

                    Logger.Information(
                        "Port successfully opened, Port: {0}, Attempts: {1}, AttemptTime: {2}, TotalTime: {3}",
                        Port,
                        attempt,
                        sw.ElapsedMilliseconds,
                        stopwatch.ElapsedMilliseconds
                    );
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Debug("Failed to connect, Port: {0}, Attempts: {1}, AttemptTime: {2}. Error: {3}", Port, attempt, sw.ElapsedMilliseconds, ex);

                    innerException = ex.ToString();
                }
            }

            Thread.Sleep(_Settings.GridServerWaitForTcpSleepInterval);

            sw.Reset();
            sw.Start();
        }

        throw new Exception(
            string.Format(
                "Failed to connect to port {0} in {1} attempts. Total time taken = {2} ms. container cannot be used. Inner Exception: {3}",
                Port,
                _Settings.GridServerStartAttempts,
                stopwatch.ElapsedMilliseconds,
                innerException
            )
        );
    }

    /// <inheritdoc cref="IGridServerInstance.UpdateResourceLimits(double, long, long)"/>
    public void UpdateResourceLimits(double maximumCores, long maximumThreads, long maximumMemoryInMegabytes)
    {
        MaximumCores = maximumCores;
        MaximumThreads = maximumThreads;
        MaximumMemoryInMegabytes = maximumMemoryInMegabytes;
    }

    private string GetVersionFromGridServer()
    {
        using var soapInterface = GetSoapInterface(_TimeoutToQueryForVersion);

        return soapInterface.GetVersion();
    }
}
