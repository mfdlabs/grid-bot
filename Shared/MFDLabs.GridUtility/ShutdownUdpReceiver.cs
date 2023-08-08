using System.Net;
using System.Text;
using System.Net.Sockets;

using Discord;

using Logging;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class ShutdownUdpReceiver
    {
        private static bool _isRunning = true;

        public static void Stop() => _isRunning = false;

        public static void Receive()
        {
            var sock = new UdpClient(new IPEndPoint(IPAddress.Loopback, 47001));

            Logger.Singleton.Debug("Receiving shutdown requests on 127.0.0.1:47001");

            var sender = new IPEndPoint(IPAddress.Loopback, 0);

            while (_isRunning)
            {
                var ver = sock.Receive(ref sender);

                Logger.Singleton.Warning("Received a shutdown request.");
                GridServerArbiter.Singleton.KillAllInstances();

                var message = $"Deploying new version ({Encoding.ASCII.GetString(ver)})";

                global::MFDLabs.Grid.Bot.Properties.Settings.Default["IsEnabled"] = false;
                global::MFDLabs.Grid.Bot.Properties.Settings.Default["ReasonForDying"] = message;

                global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();

                global::MFDLabs.Grid.Bot.Global.BotRegistry.Client.SetStatusAsync(UserStatus.DoNotDisturb).Wait();
                global::MFDLabs.Grid.Bot.Global.BotRegistry.Client.SetGameAsync(
                    $"Maintenance is enabled: {message}",
                    null,
                    ActivityType.Playing
                ).Wait();
            }
        }
    }
}
