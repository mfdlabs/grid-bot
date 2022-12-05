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
    public static class GridServerArbiterScreenshotUtility
    {
        // In the format of {guildId}:{channelId}:{userId}:{messageId}

        // guildId -> the ID of the guild (or ID of the user if we are in a DMChannel)
        // channelId -> the ID of the guild channel (or ID of the user if we are in a DMChannel)
        // userId -> the ID of the author who executed the work item.
        // messageId -> the ID of the message that used the command
        private static readonly ConcurrentDictionary<string, GridServerArbiter.LeasedGridServerInstance> SavedInstances = new();

        // This refers to each message ID that a user used the ;x command on:
        // Key -> {guildId}:{channelId}:{userId}
        private static readonly ConcurrentDictionary<string, ICollection<ulong>> UserMessageIds = new();

        // Would be better if we can embed a link to the orignal script link
        // i.e.: {guildId}:{channelId}:{userId}:{messageId} -> http://discord.com/channels/guildId/channelId/messageId (if a user channel, make it a reference to /channels/@me/botUserId/messageId)
        private static readonly ConcurrentDictionary<string, string> ScriptReferenceLookupTable = new();

        private static string ConstructBaseItemKey(this SocketMessage message)
        {
            var channel = message.Channel as SocketGuildChannel;
            var channelId = channel?.Id ?? message.Channel.Id;
            var guildId = channel != null ? channel.Guild.Id : message.Channel.Id;
            var userId = message.Author.Id;

            return $"{guildId}:{channelId}:{userId}";
        }

        private static string ConstructItemKey(this SocketMessage message, ulong? messageId = null)
        {
            return $"{message.ConstructBaseItemKey()}:{(messageId ?? message.Id)}";
        }

        private static bool GridServerInstanceAlreadyExists(GridServerArbiter.LeasedGridServerInstance inst)
        {
            return (from gInstance in SavedInstances where gInstance.Value == inst select gInstance.Value).FirstOrDefault() != null;
        }

        private static void AppendToUserMessageIdTable(this SocketMessage message)
        {
            var key = message.ConstructBaseItemKey();

            UserMessageIds.AddOrUpdate(key, _ => new ulong[] { message.Id }, (_, old) =>
            {

                var @new = old.ToList();

                @new.Add(message.Id);

                return @new;
            });
        }

        private static GridServerArbiter.LeasedGridServerInstance GetInstanceByMessage(this SocketMessage message, ulong messageId)
        {
            if (SavedInstances.TryGetValue(message.ConstructItemKey(messageId), out var inst)) return inst;

            return null;
        }

        private static string GetInstanceReferenceUrl(this SocketMessage message, ulong messageId)
        {
            if (ScriptReferenceLookupTable.TryGetValue(message.ConstructItemKey(messageId), out var url)) return url;

            return null;
        }

        public static void CreateGridServerInstanceReference(this SocketMessage message, ref GridServerArbiter.LeasedGridServerInstance inst)
        {
            if (GridServerInstanceAlreadyExists(inst)) return;

            var key = message.ConstructItemKey();

            SavedInstances.TryAdd(key, inst);
            message.AppendToUserMessageIdTable();
            ScriptReferenceLookupTable.TryAdd(key, message.GetJumpUrl());
            inst.SubscribeExpirationListener(OnLeasedExpired);
            inst.Lock();
        }

        private static string GetGridServerInstanceKey(GridServerArbiter.LeasedGridServerInstance inst)
        {
            return (from x in SavedInstances where x.Value == inst select x.Key).FirstOrDefault();
        }

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

            UserMessageIds.AddOrUpdate(baseKey, _ => Array.Empty<ulong>(), (_, old) =>
            {
                var @new = old.ToList();

                @new.Remove(messageId);

                return @new;
            });

            SavedInstances.TryRemove(instanceKey, out _);
            ScriptReferenceLookupTable.TryRemove(instanceKey, out _);
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

        private static bool CheckIfHasRecentExecutions(this SocketMessage self, out ICollection<ulong> d)
        {
            var key = self.ConstructBaseItemKey();

            if (UserMessageIds.TryGetValue(key, out d))
            {
                return d.Count > 0;
            }

            return false;
        }

        public enum ScreenshotStatus
        {
            NoRecentExecutions,
            UnknownMessageId,
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

        public static (Stream stream, string fileName, ScreenshotStatus status, GridServerArbiter.LeasedGridServerInstance instance) ScreenshotGridServer(this SocketMessage message, ulong messageId)
        {
            if (!message.CheckIfHasRecentExecutions(out var messageIds)) return (null, null, ScreenshotStatus.NoRecentExecutions, null);
            if (!messageIds.Contains(messageId)) return (null, null, ScreenshotStatus.UnknownMessageId, null);

            var gridInstance = message.GetInstanceByMessage(messageId);

            if (gridInstance == null) return (null, null, ScreenshotStatus.NullInstance, null);
            if (gridInstance.IsDisposed) return (null, null, ScreenshotStatus.DisposedInstance, null);

            var stream = GetScreenshotStream(gridInstance);

            return (stream, $"{gridInstance.Name}.png", ScreenshotStatus.Success, gridInstance);
        }
        
        public static bool HasReachedMaximumExecutionCount(this SocketMessage message, out DateTime? nextExecutionTime)
        {
            nextExecutionTime = null;
            
            if (!message.CheckIfHasRecentExecutions(out var messageIds)) return false;
            if (messageIds.Count < 25) return false;
            
            var firstMessageId = messageIds.First();
            
            var gridInstance = message.GetInstanceByMessage(firstMessageId);
            
            nextExecutionTime = gridInstance?.Expiration;
            
            return true;
        }

        public static Embed ConstructUserLookupEmbed(this SocketMessage message)
        {
            if (!message.CheckIfHasRecentExecutions(out var messageIds)) return null;

            var builder = new EmbedBuilder()
                .WithTitle("Your Recent Script Exections");

            var text = "";

            foreach (var messageId in messageIds)
            {
                var jumpUrl = message.GetInstanceReferenceUrl(messageId);
                builder.AddField($"Message Id: {messageId}", $"[Jump To Message]({jumpUrl})", true);
            }

            builder.WithAuthor(message.Author);
            builder.WithDescription(text);
            builder.WithCurrentTimestamp();
            builder.WithColor(0x00, 0xff, 0x00);

            return builder.Build();
        }
    }
}
