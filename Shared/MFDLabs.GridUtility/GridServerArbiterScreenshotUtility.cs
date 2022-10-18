/* Copyright MFDLABS Corporation. All rights reserved. */

#if NETFRAMEWORK

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
        // In the format of {guildId}:{channelId}:{userId}:{number??}
        // Refer to GRIDBOT-87 to check what we decided on.

        // guildId -> the ID of the guild (or ID of the user if we are in a DMChannel)
        // channelId -> the ID of the guild channel (or ID of the user if we are in a DMChannel)
        // userId -> the ID of the author who executed the work item.
        // number -> an atomic number that increments and decrements per allocation and expiration? -- This one is still debated
        private static readonly ConcurrentDictionary<string, GridServerArbiter.LeasedGridServerInstance> SavedInstances = new();

        // This refers to each grid server instance id that is owned by a user:
        // Key -> {guildId}:{channelId}:{userId}
        // TODO: Maybe instead of a collection of integers, it could be an atomic?
        private static readonly ConcurrentDictionary<string, ICollection<int>> UserAllocatedIds = new();

        // Would be better if we can embed a link to the orignal script link
        // i.e.: {guildId}:{channelId}:{userId}:{number??} -> http://discord.com/channels/guildId/channelId/messageId (if a user channel, make it a reference to /channels/@me/botUserId/messageId)
        private static readonly ConcurrentDictionary<string, (ulong, string)> ScriptReferenceLookupTable = new();

        private static string ConstructBaseItemKey(this SocketMessage message)
        {
            var channel = message.Channel as SocketGuildChannel;
            var channelId = channel?.Id ?? message.Channel.Id;
            var guildId = channel != null ? channel.Guild.Id : message.Channel.Id;
            var userId = message.Author.Id;

            return $"{guildId}:{channelId}:{userId}";
        }

        private static string ConstructItemKey(this SocketMessage message, int number)
        {
            return $"{message.ConstructBaseItemKey()}:{number}";
        }

        private static bool GridServerInstanceAlreadyExists(GridServerArbiter.LeasedGridServerInstance inst)
        {
            return (from gInstance in SavedInstances where gInstance.Value == inst select gInstance.Value).FirstOrDefault() != null;
        }

        private static void AppendToUserCountTable(this SocketMessage message, int number)
        {
            var key = message.ConstructBaseItemKey();

            UserAllocatedIds.AddOrUpdate(key, _ => Array.Empty<int>(), (_, old) =>
            {

                var @new = old.ToList();

                @new.Add(number);

                return @new;
            });
        }

        private static int GetCurrentIndexForGridServer(this SocketMessage message)
        {
            var userAllocatedIds = UserAllocatedIds.GetOrAdd(message.ConstructBaseItemKey(), Array.Empty<int>());

            return userAllocatedIds.LastOrDefault();
        }

        private static GridServerArbiter.LeasedGridServerInstance GetInstanceByMessage(this SocketMessage message, int number)
        {
            if (SavedInstances.TryGetValue(message.ConstructItemKey(number), out var inst)) return inst;

            return null;
        }

        private static (ulong, string) GetInstanceReferenceUrl(this SocketMessage message, int number)
        {
            if (ScriptReferenceLookupTable.TryGetValue(message.ConstructItemKey(number), out var tup)) return tup;

            return (0, null);
        }

        public static void CreateGridServerInstanceReference(this SocketMessage message, ref GridServerArbiter.LeasedGridServerInstance inst)
        {
            if (GridServerInstanceAlreadyExists(inst)) return;

            var currentId = message.GetCurrentIndexForGridServer();
            currentId++;

            var key = message.ConstructItemKey(currentId);

            SavedInstances.TryAdd(key, inst);
            message.AppendToUserCountTable(currentId);
            ScriptReferenceLookupTable.TryAdd(key, (message.Id, message.GetJumpUrl()));
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
            var number = split[3].ToInt32();

            var baseKey = $"{guildId}:{channelId}:{userId}";

            UserAllocatedIds.AddOrUpdate(baseKey, _ => Array.Empty<int>(), (_, old) =>
            {
                var @new = old.ToList();

                @new.Remove(number);

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

        private static bool CheckIfHasIds(this SocketMessage self, out ICollection<int> d)
        {
            var key = self.ConstructBaseItemKey();

            if (UserAllocatedIds.TryGetValue(key, out d))
            {
                return d.Count > 0;
            }

            return false;
        }

        public enum ScreenshotStatus
        {
            NoRecentExecutions,
            UnkownId,
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

        public static (Stream stream, string fileName, ScreenshotStatus status, GridServerArbiter.LeasedGridServerInstance instance) ScreenshotGridServer(this SocketMessage message, int index)
        {
            if (!message.CheckIfHasIds(out var ids)) return (null, null, ScreenshotStatus.NoRecentExecutions, null);
            if (!ids.Contains(index)) return (null, null, ScreenshotStatus.UnkownId, null);

            var gridInstance = message.GetInstanceByMessage(index);

            if (gridInstance == null) return (null, null, ScreenshotStatus.NullInstance, null);
            if (gridInstance.IsDisposed) return (null, null, ScreenshotStatus.DisposedInstance, null);

            var stream = GetScreenshotStream(gridInstance);

            return (stream, $"{gridInstance.Name}.png", ScreenshotStatus.Success, gridInstance);
        }

        public static Embed ConstructUserLookupEmbed(this SocketMessage message)
        {
            if (!message.CheckIfHasIds(out var ids)) return null;

            var builder = new EmbedBuilder()
                .WithTitle("Your Grid Server Instances");

            var text = "";

            foreach (var id in ids)
            {
                var (messageId, jumpUrl) = message.GetInstanceReferenceUrl(id);
                builder.AddField($"Instance ID {id}", $"[{messageId}]({jumpUrl})", true);
            }

            builder.WithAuthor(message.Author);
            builder.WithDescription(text);
            builder.WithCurrentTimestamp();
            builder.WithColor(0x00, 0xff, 0x00);

            return builder.Build();
        }
    }
}

#endif