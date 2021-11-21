using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Logging;
using MFDLabs.Networking;
using MFDLabs.Text.Extensions;

// TODO: Move to a task thread

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

                bool isAdminScript = global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminScripts && userIsAdmin;

                var scriptID = NetworkingGlobal.Singleton.GenerateUUIDV4();
                var filesafeScriptID = scriptID.Replace("-", "");
                var scriptName = GridServerCommandUtility.Singleton.GetGridServerScriptPath(filesafeScriptID);

                // isAdmin allows a bypass of disabled methods and virtualized globals
                var (command, settings) = JsonScriptingUtility.Singleton.GetSharedGameServerExecutionScript(
                    filesafeScriptID,
                    ("isAdmin", isAdminScript)
                );

                if (isAdminScript) SystemLogger.Singleton.Debug("Admin scripts are enabled, disabling VM.");

                try
                {
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionRequireProtections)
                        script = $"{LuaUtility.Singleton.SafeLuaMode}{script}";

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionPrependBaseURL)
                        script = $"game:GetService(\"ContentProvider\"):SetBaseUrl(\"{MFDLabs.Grid.Bot.Properties.Settings.Default.BaseURL}\");{script}";

                    File.WriteAllText(scriptName, script, Encoding.ASCII);

                    var scriptEx = Lua.NewScript(
                        NetworkingGlobal.Singleton.GenerateUUIDV4(),
                        command
                    );

                    // bump to 20 seconds so it doesn't batch job timeout on first execution
                    var job = new Job() { id = scriptID, expirationInSeconds = userIsAdmin ? 20000 : 20 };
                    var result = LuaUtility.Singleton.ParseLuaValues(await GridServerArbiter.Singleton.BatchJobExAsync(job, scriptEx));

                    // HACK throw here because too lazy to write more code.
                    if (result?.Length >= MaxResultLength) throw new ArgumentOutOfRangeException();

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
                }
                catch (ArgumentOutOfRangeException) { await message.ReplyAsync("The contents of the response exceeds the maximum embed description size, so it will not be returned."); }
                catch (IOException) { await message.ReplyAsync("There was an IO error when writing the script to the system, please try again later."); }
                finally
                {
                    try
                    {
                        SystemLogger.Singleton.LifecycleEvent("Trying delete the script '{0}' at path '{1}'", scriptID, scriptName);
                        File.Delete(scriptName);
                        SystemLogger.Singleton.LifecycleEvent("Successfully deleted the script '{0}' at path '{1}'!", scriptID, scriptName);
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.Singleton.Warning("Failed to delete the user script '{0}' because '{1}'", scriptName, ex?.Message);
                    }
                }
            }
        }

        private const int MaxResultLength = EmbedBuilder.MaxDescriptionLength - 8;
    }
}
