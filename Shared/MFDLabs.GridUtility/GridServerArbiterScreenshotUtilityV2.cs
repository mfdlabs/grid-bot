using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Discord;
using Discord.WebSocket;
using MFDLabs.Drawing;
using MFDLabs.FileSystem;
using MFDLabs.Reflection.Extensions;

using HWND = System.IntPtr;

namespace MFDLabs.Grid.Bot.Utility
{
    public static class GridServerArbiterScreenshotUtilityV2
    {
        // In the format of {guildId}:{channelId}:{userId}:{messageId}

        // guildId -> the ID of the guild (or ID of the user if we are in a DMChannel)
        // channelId -> the ID of the guild channel (or ID of the user if we are in a DMChannel)
        // userId -> the ID of the author who executed the work item.
        // messageId -> the ID of the message that used the command
        private static readonly ConcurrentDictionary<string, GridServerArbiter.LeasedGridServerInstance> SavedInstances = new();

        // This refers to each message ID that a user used the ;x command on:
        // Key -> {guildId}:{channelId}:{userId}
        private static readonly ConcurrentDictionary<string, ICollection<ulong>> UserSlashCommandIds = new();

        private static string ConstructBaseItemKey(this SocketSlashCommand command)
        {
            var channel = command.Channel as SocketGuildChannel;
            var channelId = channel?.Id ?? command.Channel.Id;
            var guildId = channel != null ? channel.Guild.Id : command.Channel.Id;
            var userId = command.User.Id;

            return $"{guildId}:{channelId}:{userId}";
        }

        private static string ConstructItemKey(this SocketSlashCommand command, ulong? messageId = null)
        {
            return $"{command.ConstructBaseItemKey()}:{(messageId ?? command.Id)}";
        }

        private static bool GridServerInstanceAlreadyExists(GridServerArbiter.LeasedGridServerInstance inst) 
            => (from gInstance in SavedInstances where gInstance.Value == inst select gInstance.Value).FirstOrDefault() != null;

        private static void AppendToUserSlashCommandIdTable(this SocketSlashCommand command)
        {
            var key = command.ConstructBaseItemKey();

            UserSlashCommandIds.AddOrUpdate(key, _ => new ulong[] { command.Id }, (_, old) =>
            {

                var @new = old.ToList();

                @new.Add(command.Id);

                return @new;
            });
        }

        private static GridServerArbiter.LeasedGridServerInstance GetInstanceBySlashCommand(this SocketSlashCommand command, ulong messageId)
        {
            if (SavedInstances.TryGetValue(command.ConstructItemKey(messageId), out var inst)) return inst;

            return null;
        }

        public static void CreateGridServerInstanceReference(this SocketSlashCommand command, ref GridServerArbiter.LeasedGridServerInstance inst)
        {
            if (GridServerInstanceAlreadyExists(inst)) return;

            var key = command.ConstructItemKey();

            SavedInstances.TryAdd(key, inst);
            command.AppendToUserSlashCommandIdTable();
            inst.SubscribeExpirationListener(OnLeasedExpired);
            inst.Lock();
        }

        private static string GetGridServerInstanceKey(GridServerArbiter.LeasedGridServerInstance inst) 
            => (from x in SavedInstances where x.Value == inst select x.Key).FirstOrDefault();

        private static void OnLeasedExpired(GridServerArbiter.LeasedGridServerInstance inst)
        {
            var instanceKey = GetGridServerInstanceKey(inst);

            if (instanceKey == null) return;

            var split = instanceKey.Split(':');

            var guildId = split[0];
            var channelId = split[1];
            var userId = split[2];
            var messageId = split[3].ToUInt64();

            var baseKey = $"{guildId}:{channelId}:{userId}";

            UserSlashCommandIds.AddOrUpdate(baseKey, _ => Array.Empty<ulong>(), (_, old) =>
            {
                var @new = old.ToList();

                @new.Remove(messageId);

                return @new;
            });

            SavedInstances.TryRemove(instanceKey, out _);

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScreenshotUtilityLaunchGridServerOnLeaseExpired)
                GridServerArbiter.Singleton.BatchQueueUpLeasedArbiteredInstancesUnsafe(
                    null,
                    1
#if DEBUG
                    ,
                    5,
                    "localhost",
                    false
#endif
                );
        }

        private static bool CheckIfHasRecentExecutions(this SocketSlashCommand self, out ICollection<ulong> d)
        {
            var key = self.ConstructBaseItemKey();

            if (UserSlashCommandIds.TryGetValue(key, out d))
            {
                return d.Count > 0;
            }

            return false;
        }

        public enum ScreenshotStatus
        {
            NoRecentExecutions,
            UnknownSlashCommandId,
            NullInstance,
            DisposedInstance,
            Success
        }

        private static void MaximizeGridServer([In] HWND hWnd)
        {
            const int SW_MAXIMIZE = 3;

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool ShowWindow(HWND hWnd, int nCmdShow);

            ShowWindow(hWnd, SW_MAXIMIZE);
        }

        private static Stream GetScreenshotStream(GridServerArbiter.LeasedGridServerInstance inst)
        {
            var tempFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", $"{inst.Name}");
            try
            {
                var mainWindowHandle = Process.GetProcessById(inst.ProcessId).MainWindowHandle;
                MaximizeGridServer(mainWindowHandle);
                var bitMap = mainWindowHandle.GetBitmapForWindowByWindowHandle();
                bitMap.Save(tempFileName);
                return new MemoryStream(File.ReadAllBytes(tempFileName));
            }
            finally
            {
                tempFileName.PollDeletion();
            }
        }

        public static (Stream stream, string fileName, ScreenshotStatus status, GridServerArbiter.LeasedGridServerInstance instance) ScreenshotGridServer(this SocketSlashCommand command, ulong slashCommandId)
        {
            if (!command.CheckIfHasRecentExecutions(out var slashCommandIds)) return (null, null, ScreenshotStatus.NoRecentExecutions, null);
            if (!slashCommandIds.Contains(slashCommandId)) return (null, null, ScreenshotStatus.UnknownSlashCommandId, null);

            var gridInstance = command.GetInstanceBySlashCommand(slashCommandId);

            if (gridInstance == null) return (null, null, ScreenshotStatus.NullInstance, null);
            if (gridInstance.IsDisposed) return (null, null, ScreenshotStatus.DisposedInstance, null);

            var stream = GetScreenshotStream(gridInstance);

            return (stream, $"{gridInstance.Name}.png", ScreenshotStatus.Success, gridInstance);
        }
        
        public static bool HasReachedMaximumExecutionCount(this SocketSlashCommand command, out DateTime? nextExecutionTime)
        {
            nextExecutionTime = null;
            
            if (!command.CheckIfHasRecentExecutions(out var slashCommandIds)) return false;
            if (slashCommandIds.Count < 25) return false;
            
            var firstSlashCommandId = slashCommandIds.First();
            
            var gridInstance = command.GetInstanceBySlashCommand(firstSlashCommandId);
            
            nextExecutionTime = gridInstance?.Expiration;
            
            return true;
        }

        public static Embed ConstructUserLookupEmbed(this SocketSlashCommand command)
        {
            if (!command.CheckIfHasRecentExecutions(out var slashCommandIds)) return null;

            var builder = new EmbedBuilder()
                .WithTitle("Your Recent Script Exections");

            var text = "";

            foreach (var slashCommandId in slashCommandIds)
            {
                var gridServer = command.GetInstanceBySlashCommand(slashCommandId);
                builder.AddField($"Slash Command Id: {slashCommandId}", $"Grid Server {gridServer.Name}, expires at {gridServer.Expiration}", true);
            }

            builder.WithAuthor(command.User);
            builder.WithDescription(text);
            builder.WithCurrentTimestamp();
            builder.WithColor(0x00, 0xff, 0x00);

            return builder.Build();
        }
    }
}
