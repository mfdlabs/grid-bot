namespace Grid.Bot.Utility;

using System.Net;
using System.Text;
using System.Net.Sockets;

using Discord;

using Logging;

using Global;

/// <summary>
/// UDP receiver for the auto-deployer shutdown request.
/// </summary>
public sealed class AutoDeployerUpgradeReciever
{
    /// <summary>
    /// Start receiving the message from the auto-deployer.
    /// </summary>
    public static void Receive()
    {
        var sock = new UdpClient(new IPEndPoint(IPAddress.Loopback, 47001));

        Logger.Singleton.Debug("Receiving shutdown requests on 127.0.0.1:47001");

        var sender = new IPEndPoint(IPAddress.Loopback, 0);

        var ver = sock.Receive(ref sender);

        Logger.Singleton.Warning("Received a shutdown request.");
        GridServerArbiter.Singleton.KillAllInstances();
        ScriptExecutionArbiter.Singleton.KillAllInstances();

        var message = $"Deploying new version ({Encoding.ASCII.GetString(ver)})";

        MaintenanceSettings.Singleton.MaintenanceEnabled = true;
        MaintenanceSettings.Singleton.MaintenanceStatus = message;

        BotRegistry.Client.SetStatusAsync(UserStatus.DoNotDisturb).Wait();
        BotRegistry.Client.SetGameAsync(
            $"Maintenance is enabled: {message}",
            null,
            ActivityType.Playing
        ).Wait();
    }
}
