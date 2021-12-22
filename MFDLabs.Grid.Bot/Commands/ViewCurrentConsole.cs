﻿using System;
using System.IO;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Drawing;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.NativeWin32;
using MFDLabs.Threading;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class ViewCurrentConsole : IStateSpecificCommandHandler
    {
        public string CommandName => "View Current Bot Instance Console";
        public string CommandDescription => "Attempts to take a screenshot of the bot's console output.";
        public string[] CommandAliases => new[] { "vcc", "viewcurrentconsole" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var currentMainwindowHandle = NativeMethods.GetConsoleWindow();

            if (currentMainwindowHandle == IntPtr.Zero)
            {
                await message.ReplyAsync($"The running app has no window, therefore nothing to screenshot. " +
                                         $"Please find the machine '{SystemGlobal.GetMachineId()}' to get the console output.");
                return;
            }

            var bitmap = currentMainwindowHandle.GetBitmapForWindowByWindowHandle();
            bitmap.Save("ServerShot.png");

            await message.Channel.SendFileAsync("ServerShot.png");

            TaskHelper.SetTimeout(() =>
            {
                File.Delete("ServerShot.png");
            }, TimeSpan.FromSeconds(2));
        }
    }
}
