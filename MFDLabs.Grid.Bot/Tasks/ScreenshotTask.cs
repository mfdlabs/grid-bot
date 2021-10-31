using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Concurrency;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Base;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Plugins;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Logging;
using MFDLabs.Threading;

namespace MFDLabs.Grid.Bot.Tasks
{
    internal sealed class ScreenshotTask : HoldsSocketMessagePortAsyncExpiringTaskThread<ScreenshotTask>
    {
        public override string Name => "Screenshot Relay";
        public override TimeSpan ProcessActivationInterval => global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayActivationTimeout;
        public override int PacketID => 2;

        public override TimeSpan Expiration => global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayExpiration;

        /* Will execute in a different thread when dispatching response. */
        public override async Task<PluginResult> OnReceive(Packet<SocketMessage> packet)
        {
            var metrics = await AsyncPacketMetricsPlugin<SocketMessage>.Singleton.OnReceive(packet);

            if (metrics == PluginResult.StopProcessingAndDeallocate) return PluginResult.StopProcessingAndDeallocate;

            if (packet.Item != null)
            {
                if (packet.Item.Content.Contains("-test---test-123-okkkk") && packet.Item.Author.IsAdmin()) throw new Exception("Test exception for auto handling on task threads.");


                using (packet.Item.Channel.EnterTypingState())
                {
                    var tte = SystemUtility.Singleton.OpenGridServer().Item1;

                    if (tte.TotalSeconds > 1.5)
                    {
                        // Wait for 1.25s so the grid server output can be populated.
                        TaskHelper.SetTimeout(async () => await DispatchSocketMessage(packet), TimeSpan.FromMilliseconds(1250));
                    }
                    else
                    {
                        await DispatchSocketMessage(packet);
                    }
                }
                return PluginResult.ContinueProcessing;
            }
            else
            {
                SystemLogger.Singleton.Warning("Task packet {0} at the sequence {1} had a null item, ignoring...", packet.ID, packet.SequenceID);
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.StopProcessingOnNullPacketItem) return PluginResult.StopProcessingAndDeallocate;
                return PluginResult.ContinueProcessing;
            }

        }

        private async Task DispatchSocketMessage(IPacket<SocketMessage> packet)
        {
            if (!TryExecuteScreenshotRelay())
            {
                await packet.Item.ReplyAsync("An error occurred when invoking a screenshot relay request.");
            }

            await packet.Item.Channel.SendFileAsync(
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayOutputFilename
            );

            TaskHelper.SetTimeout(() =>
            {
                try
                {
                    File.Delete(global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayOutputFilename);
                }
                catch { }
            }, TimeSpan.FromSeconds(2));
        }

        private bool TryExecuteScreenshotRelay()
        {
            var sw = Stopwatch.StartNew();
            SystemLogger.Singleton.Log("Try screenshot Grid Server");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayExecutableName,
                };

                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayShouldShowLauncherWindow)
                {
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                }

                if (SystemGlobal.Singleton.ContextIsAdministrator())
                {
                    psi.Verb = "runas";
                }

                var proc = new Process
                {
                    StartInfo = psi
                };

                proc.Start();
                proc.WaitForExit();

                SystemLogger.Singleton.Info(
                    "Successfully executed screenshot of Grid Server via {0}",
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayExecutableName
                );
                return true;
            }
            catch (Exception ex)
            {
                SystemLogger.Singleton.Error(ex);
                return false;
            }
            finally
            {
                SystemLogger.Singleton.Debug(
                    "Took {0}s to execute screenshot of via {1}",
                    sw.Elapsed.TotalSeconds.ToString("f7"),
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotRelayExecutableName
                );
                sw.Stop();
            }
        }
    }
}
