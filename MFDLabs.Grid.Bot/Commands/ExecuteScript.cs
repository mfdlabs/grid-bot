using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
using MFDLabs.Text.Extensions;

// TODO: Support reading from files

namespace MFDLabs.Grid.Bot.Commands
{
    internal class ExecuteScript : IStateSpecificCommandHandler
    {
        public string CommandName => "Execute Grid Server Lua Script";
        public string CommandDescription => $"Attempts to execute the given script contents on a grid server instance\nLayout: {MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}execute ...script.";
        public string[] CommandAliases => new string[] { "x", "ex", "execute" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            using (message.Channel.EnterTypingState())
            {
                var userIsAdmin = message.Author.IsAdmin();
                var userIsOwner = message.Author.IsOwner();

                var script = string.Join(" ", messageContentArray);



                if (script.IsNullWhiteSpaceOrEmpty())
                {
                    // let's try and read the attachments.
                    if (message.Attachments.Count == 0)
                    {
                        await message.ReplyAsync("The script or at least 1 attachment was expected.");
                        return;
                    }

                    var firstAttachment = message.Attachments.First();
                    if (!firstAttachment.Filename.EndsWith(".lua"))
                    {
                        await message.ReplyAsync("The attachment is required to be a valid Lua file.");
                        return;
                    }

                    script = message.Attachments.First().GetAttachmentContentsAscii();
                }

                // remove phone specific quotes (why would you want them anyway? they are unicode)
                script = script.EscapeQuotes();
                script = script.GetCodeBlockContents();

                if (LuaUtility.Singleton.CheckIfScriptContainsDisallowedText(script, out string keyword) && !userIsAdmin)
                {
                    await message.ReplyAsync($"The script you sent contains keywords that are not permitted, please review your script and change the blacklisted keyword: {keyword}");
                    return;
                }

                if (script.ContainsUnicode() && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionSupportUnicode && !userIsAdmin)
                {
                    await message.ReplyAsync("Sorry, but unicode in messages is not supported as of now, please remove any unicode characters from your script.");
                    return;
                }

                var (command, settings) = JsonScriptingUtility.Singleton.GetSharedGameServerExecutionScript("run", new Dictionary<string, object>() { { "script", script.Escape() } });

                bool didWriteAdminScript = false;

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminScripts && SystemGlobal.Singleton.ContextIsAdministrator() && settings is ExecuteScriptGameServerSettings)
                {
                    if (userIsAdmin)
                    {
                        SystemLogger.Singleton.LifecycleEvent("The user '{0}' is an admin and the setting 'AllowAdminScripts' is enabled.", message.Author.Id);

                        await message.Channel.SendMessageAsync("Executing script with higher permissions.");

                        bool allow = true;

                        if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.AdminScriptsOnlyAllowedByOwner && !userIsOwner) allow = false;

                        if (allow)
                        {
                            try
                            {
                                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.AdminScriptPrependBaseURL)
                                {
                                    script = $"game:GetService(\"ContentProvider\"):SetBaseUrl(\"{MFDLabs.Grid.Bot.Properties.Settings.Default.BaseURL}\");\n{script}";
                                }

                                var adminScriptPath = GridServerCommandUtility.Singleton.GetGridServerScriptPath("admin");

                                SystemLogger.Singleton.Debug("Writing admin script to path '{0}'", adminScriptPath);

                                lock (_writerLock)
                                    File.WriteAllText(adminScriptPath, script, Encoding.ASCII);

                                didWriteAdminScript = true;

                                (command, settings) = JsonScriptingUtility.Singleton.GetSharedGameServerExecutionScript("admin");

                            }
                            catch (ApplicationException ex)
                            {
                                await message.ReplyAsync(ex.Message);
                                return;
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

                // bump to 20 seconds so it doesn't batch job timeout on first execution
                var job = new Job() { id = NetworkingGlobal.Singleton.GenerateUUIDV4(), expirationInSeconds = didWriteAdminScript ? 20000 : 20 };
                var result = LuaUtility.Singleton.ParseLuaValues(await GridServerArbiter.Singleton.BatchJobExAsync(job, scriptEx));

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
                    try
                    {
                        lock (_writerLock)
                            File.Delete(GridServerCommandUtility.Singleton.GetGridServerScriptPath("admin"));
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
