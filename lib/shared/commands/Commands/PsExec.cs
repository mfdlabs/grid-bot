using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class PsExec : IStateSpecificCommandHandler
    {
        public string CommandName => "Execute PowerShell";
        public string CommandDescription => $"Attempts to evaluate the given Powershell\nLayout:" +
                                            $"{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix)}psexec ...command.";
        public string[] CommandAliases => new[] { "ps", "psexec" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotOwnerAsync()) return;

            var scriptContents = messageContentArray.Join(' ');
            if (scriptContents.IsNullWhiteSpaceOrEmpty())
            {
                // let's try and read the attachments.
                if (message.Attachments.Count == 0)
                {
                    await message.ReplyAsync("The script or at least 1 attachment was expected.");
                    return;
                }

                var firstAttachment = message.Attachments.First();

                scriptContents = firstAttachment.GetAttachmentContentsAscii();
            }

            scriptContents = scriptContents.EscapeQuotes().GetCodeBlockContents();


            using (message.Channel.EnterTypingState())
            {
                Process result;

                try
                {
                    result = new Process();
                    result.StartInfo.FileName = "powershell.exe";
                    result.StartInfo.Arguments = $"-ExecutionPolicy Unrestricted -Command {scriptContents}";
                    result.StartInfo.UseShellExecute = false;
                    result.StartInfo.RedirectStandardOutput = true;
                    result.StartInfo.RedirectStandardError = true;
                    result.Start();
                    result.WaitForExit(20000);
                }
                catch (Exception ex)
                {
                    await HandleException(message, ex);
                    return;
                }

                var returnValue = await result.StandardOutput.ReadToEndAsync();
                var errorValue = await result.StandardError.ReadToEndAsync();

                if (!errorValue.IsNullOrEmpty())
                {
                    await HandleException(message, new Exception(errorValue));
                    return;
                }

                if (!returnValue.IsNullOrEmpty())
                {
                    if (returnValue.Length > EmbedBuilder.MaxDescriptionLength)
                    {
                        await message.ReplyWithFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(returnValue)),
                            "psexec.txt", "Executed script with return:");
                        return;
                    }
                    await message.ReplyAsync(
                        "Executed script with return:",
                        embed: new EmbedBuilder()
                        .WithTitle("Return value")
                        .WithDescription($"```\n{returnValue}\n```")
                        .WithAuthor(message.Author)
                        .WithCurrentTimestamp()
                        .WithColor(0x00, 0xff, 0x00)
                        .Build()
                    );
                    return;
                }
                await message.ReplyAsync("Executed script with no return!");
            }
        }

        private static async Task HandleException(SocketMessage message, Exception ex)
        {
            await message.ReplyAsync("an exception occurred when trying to execute the given PowerShell, please " +
                                     "review this error to see if your input was malformed:");

            if (ex.Message.Length > EmbedBuilder.MaxDescriptionLength)
            {
                await message.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(ex.Message)), "psexec-error.txt");
                return;
            }

            await message.Channel.SendMessageAsync(
                embed: new EmbedBuilder()
                .WithColor(0xff, 0x00, 0x00)
                .WithTitle("Execute exception.")
                .WithAuthor(message.Author)
                .WithDescription($"```\n{ex.Message}\n```")
                .Build()
            );
        }
    }
}
