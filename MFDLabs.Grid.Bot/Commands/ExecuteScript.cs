using Discord;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Grid.Commands;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Logging;
using MFDLabs.Networking;
using MFDLabs.Text;
using MFDLabs.Text.Extensions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class ExecuteScript : IStateSpecificCommandHandler
    {
        public string CommandName => "Execute Script";

        public string CommandDescription => "Dispataches the given lua and executes it on the grid server.";

        public string[] CommandAliases => new string[] { "x", "ex", "execute" };

        public bool Internal => !Settings.Singleton.ScriptExectionEnabled;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!Settings.Singleton.ScriptExectionEnabled)
            {
                if (!await message.RejectIfNotAdminAsync()) return;
            }

            using (message.Channel.EnterTypingState())
            {
                var script = string.Join(" ", messageContentArray);

                script = Regex.Replace(script, "[\"“‘”]", "\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                // todo: try and dynamically remove code blocks
                //script = Regex.Replace(script, @"```(?:(?<lang>\S+)\n)?\s?(?<code>[^]+?)\s?```", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                script = script.Replace("`", "");

                if (script.IsNullWhiteSpaceOrEmpty())
                {
                    await message.ReplyAsync("The script is actually required by the way.");
                    return;
                }

                // allow admins to bypass?
                if (LuaUtility.Singleton.CheckIfScriptContainsDisallowedText(script, out string keyword))
                {
                    await message.ReplyAsync($"The script you sent contains keywords that are not permitted, please review your script and change the blacklisted keyword: {keyword}");
                    return;
                }

                // allow admins to bypass?
                if (TextGlobal.Singleton.StringContainsUnicode(script) && !Settings.Singleton.ScriptExectionSupportUnicode)
                {
                    await message.ReplyAsync("Sorry, but unicode in messages is not supported as of now, please remove any unicode characters from your script.");
                    return;
                }

                // This is ugly as hell, can we have empty constructors so we can not include some stuff pls?
                // jakob: just add shared ones maybe
                var (command, settings) = JsonScriptingUtility.Singleton.GetSharedGameServerExecutionScript("run", new Dictionary<string, object>() { { "script", TextGlobal.Singleton.EscapeString(script) } });

                bool didWriteAdminScript = false;

                if (Settings.Singleton.AllowAdminScripts && SystemGlobal.Singleton.ContextIsAdministrator() && settings is ExecuteScriptGameServerSettings)
                {
                    if (AdminUtility.Singleton.UserIsAdmin(message.Author))
                    {
                        SystemLogger.Singleton.LifecycleEvent("The user '{0}' is an admin and the setting 'AllowAdminScripts' is enabled.", message.Author.Id);

                        await message.Channel.SendMessageAsync("Executing script with higher permissions.");

                        bool allow = true;

                        if (Settings.Singleton.AdminScriptsOnlyAllowedByOwner && !AdminUtility.Singleton.CheckIsUserOwner(message.Author)) allow = false;

                        if (allow)
                        {
                            try
                            {
                                var gridServicePath = Registry.GetValue(
                                    Settings.Singleton.GridServerRegistryKeyName,
                                    Settings.Singleton.GridServerRegistryValueName,
                                    null
                                );

                                if (gridServicePath == null)
                                {
                                    await message.ReplyAsync($"The grid server was not correctly installed on the machine '{SystemGlobal.Singleton.GetMachineID()}', please contact the datacenter administrator to sort this out.");
                                    return;
                                }

                                if (Settings.Singleton.AdminScriptPrependBaseURL)
                                {
                                    script = $"game:GetService(\"ContentProvider\"):SetBaseUrl(\"{Settings.Singleton.BaseURL}\");\n{script}";
                                }

                                lock (_writerLock)
                                    File.WriteAllText($"{gridServicePath}\\internalscripts\\scripts\\admin.lua", script, Encoding.ASCII);

                                didWriteAdminScript = true;

                                (command, settings) = JsonScriptingUtility.Singleton.GetSharedGameServerExecutionScript("admin", new Dictionary<string, object>());

                            }
                            catch (Exception ex)
                            {
                                SystemLogger.Singleton.Error(ex);
                                //await message.ReplyAsync($"An error occurred when trying to write an admin script to the machine '{SystemGlobal.Singleton.GetMachineID()}' GridServer path.");
                            }
                        }
                    }
                }

                var scriptEx = Lua.NewScript(
                    NetworkingGlobal.Singleton.GenerateUUIDV4(),
                    command
                );

                // bump to 15 seconds so it doesn't batch job timeout on first execution
                var job = new Job() { id = NetworkingGlobal.Singleton.GenerateUUIDV4(), expirationInSeconds = didWriteAdminScript ? 20000 : 15 };
                var result = LuaUtility.Singleton.ParseLuaValues(await SoapUtility.Singleton.BatchJobExAsync(job, scriptEx));

                await message.ReplyAsync(result.IsNullOrEmpty() ? "Executed script with no return!" : $"Executed script with return:");
                if (!result.IsNullOrEmpty())
                    await message.Channel.SendMessageAsync(
                        embed: new EmbedBuilder()
                        .WithTitle("Return value")
                        .WithDescription($"```\n{result}\n```")
                        .WithAuthor(message.Author)
                        .WithCurrentTimestamp()
                        .WithColor(0x00, 0xff, 0x00)
                        .Build()
                    );
                if (didWriteAdminScript)
                {
                    var gridServicePath = Registry.GetValue(
                        Settings.Singleton.GridServerRegistryKeyName,
                        Settings.Singleton.GridServerRegistryValueName,
                        null
                    );

                    if (gridServicePath == null)
                    {
                        await message.ReplyAsync($"The grid server was not correctly installed on the machine '{SystemGlobal.Singleton.GetMachineID()}', please contact the datacenter administrator to sort this out.");
                        return;
                    }

                    try
                    {
                        lock (_writerLock)
                            File.Delete($"{gridServicePath}\\internalscripts\\scripts\\admin.lua");
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.Singleton.Error(ex);
                    }
                }
            }
        }

        private static readonly object _writerLock = new object();
    }
}
