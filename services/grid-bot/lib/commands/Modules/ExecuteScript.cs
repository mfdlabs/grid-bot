namespace Grid.Bot.Interactions.Public;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Discord;
using Discord.Interactions;

using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;

using Logging;
using FileSystem;

using Utility;
using Commands;
using Extensions;

using GridJob = Client.Job;

/// <summary>
/// Interaction handler for executing Luau code.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="ExecuteScript"/>.
/// </remarks>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="gridSettings">The <see cref="GridSettings"/>.</param>
/// <param name="scriptsSettings">The <see cref="ScriptsSettings"/>.</param>
/// <param name="luaUtility">The <see cref="ILuaUtility"/>.</param>
/// <param name="floodCheckerRegistry">The <see cref="IFloodCheckerRegistry"/>.</param>
/// <param name="backtraceUtility">The <see cref="IBacktraceUtility"/>.</param>
/// <param name="jobManager">The <see cref="IJobManager"/>.</param>
/// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
/// <param name="discordWebhookAlertManager">The <see cref="IDiscordWebhookAlertManager"/>.</param>
/// <param name="scriptLogger">The <see cref="IScriptLogger"/>.</param>
/// <param name="gridServerFileHelper">The <see cref="IGridServerFileHelper"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="logger"/> cannot be null.
/// - <paramref name="gridSettings"/> cannot be null.
/// - <paramref name="scriptsSettings"/> cannot be null.
/// - <paramref name="luaUtility"/> cannot be null.
/// - <paramref name="floodCheckerRegistry"/> cannot be null.
/// - <paramref name="backtraceUtility"/> cannot be null.
/// - <paramref name="jobManager"/> cannot be null.
/// - <paramref name="adminUtility"/> cannot be null.
/// - <paramref name="discordWebhookAlertManager"/> cannot be null.
/// - <paramref name="scriptLogger"/> cannot be null.
/// - <paramref name="gridServerFileHelper"/> cannot be null.
/// </exception>
[Group("execute", "Commands used for executing Luau code.")]
[IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
public partial class ExecuteScript(
    ILogger logger,
    GridSettings gridSettings,
    ScriptsSettings scriptsSettings,
    ILuaUtility luaUtility,
    IFloodCheckerRegistry floodCheckerRegistry,
    IBacktraceUtility backtraceUtility,
    IJobManager jobManager,
    IAdminUtility adminUtility,
    IDiscordWebhookAlertManager discordWebhookAlertManager,
    IScriptLogger scriptLogger,
    IGridServerFileHelper gridServerFileHelper
) : InteractionModuleBase<ShardedInteractionContext>
{
    private const int _maxErrorLength = EmbedBuilder.MaxDescriptionLength - 8;
    private const int _maxResultLength = EmbedFieldBuilder.MaxFieldValueLength - 8;


    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly GridSettings _gridSettings = gridSettings ?? throw new ArgumentNullException(nameof(gridSettings));
    private readonly ScriptsSettings _scriptsSettings = scriptsSettings ?? throw new ArgumentNullException(nameof(scriptsSettings));

    private readonly ILuaUtility _luaUtility = luaUtility ?? throw new ArgumentNullException(nameof(luaUtility));
    private readonly IFloodCheckerRegistry _floodCheckerRegistry = floodCheckerRegistry ?? throw new ArgumentNullException(nameof(floodCheckerRegistry));
    private readonly IBacktraceUtility _backtraceUtility = backtraceUtility ?? throw new ArgumentNullException(nameof(backtraceUtility));
    private readonly IJobManager _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
    private readonly IDiscordWebhookAlertManager _discordWebhookAlertManager = discordWebhookAlertManager ?? throw new ArgumentNullException(nameof(discordWebhookAlertManager));
    private readonly IScriptLogger _scriptLogger = scriptLogger ?? throw new ArgumentNullException(nameof(scriptLogger));
    private readonly IGridServerFileHelper _gridServerFileHelper = gridServerFileHelper ?? throw new ArgumentNullException(nameof(gridServerFileHelper));


    [GeneratedRegex(@"```(.*?)\s(.*?)```", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();
    [GeneratedRegex("[\"“‘”]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex QuotesRegex();
    [GeneratedRegex(@"Execute Script:(\d+): (.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GridSyntaxErrorRegex();

    private const string _ErrorConvertingToJson = "Can't convert to JSON";

    /// <inheritdoc cref="InteractionModuleBase{TContext}.BeforeExecuteAsync(ICommandInfo)"/>
    public override async Task BeforeExecuteAsync(ICommandInfo command)
    {
        if (!_adminUtility.UserIsAdmin(Context.User))
        {
            if (_floodCheckerRegistry.ScriptExecutionFloodChecker.IsFlooded())
                throw new ApplicationException("Too many people are using this command at once, please wait a few moments and try again.");

            _floodCheckerRegistry.RenderFloodChecker.UpdateCount();

            var perUserFloodChecker = _floodCheckerRegistry.GetPerUserScriptExecutionFloodChecker(Context.User.Id);
            if (perUserFloodChecker.IsFlooded())
                throw new ApplicationException("You are sending execute script commands too quickly, please wait a few moments and try again.");

            perUserFloodChecker.UpdateCount();
        }

        await base.BeforeExecuteAsync(command);
    }

    private static string GetCodeBlockContents(string s)
    {
        var match = CodeBlockRegex().Match(s);

        if (match != null && match.Groups.Count == 3)
        {
            if (!s.Contains($"```{match.Groups[1].Value}\n"))
                return $"{match.Groups[1].Value} {match.Groups[2].Value}";

            return match.Groups[2].Value;
        }

        return s.Replace("`", ""); // Return the value here again?
    }

    private static string EscapeQuotes(string s) => QuotesRegex().Replace(s, "\"");

    private static bool ContainsUnicode(string s) => s.Any(c => c > 255);

    private (string, MemoryStream) DetermineDescription(string input, string fileName)
    {
        if (string.IsNullOrEmpty(input)) return (null, null);

        // Check if the input matches grid syntax error
        if (GridSyntaxErrorRegex().IsMatch(input))
        {
            var match = GridSyntaxErrorRegex().Match(input);
            var line = match.Groups[1].Value;
            var error = match.Groups[2].Value;

            input = $"Line {line}: {error}";
        }

        // Replace backticks with escaped backticks
        input = input.Replace("`", "\\`");

        if (input.Length > _maxErrorLength)
        {
            var maxSize = _scriptsSettings.ScriptExecutionMaxFileSizeKb;

            if (input.Length / 1000 > maxSize)
                return ($"The output cannot be larger than {maxSize} KiB", null);

            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        return (input, null);
    }

    private (string, MemoryStream) DetermineResult(string input, string fileName)
    {
        if (string.IsNullOrEmpty(input)) return (null, null);

        // Replace backticks with escaped backticks
        input = input.Replace("`", "\\`");

        if (input.Length > _maxResultLength)
        {
            var maxSize = _scriptsSettings.ScriptExecutionMaxResultSizeKb;

            if (input.Length / 1000 > maxSize)
                return ($"The result cannot be larger than {maxSize} KiB", null);

            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        return (input, null);
    }

    private async Task LuaErrorAsync(string error)
        => await HandleResponseAsync(null, new() { ErrorMessage = error, ExecutionTime = 0, Success = false });

    private async Task HandleResponseAsync(string result, ReturnMetadata metadata)
    {
        var builder = new EmbedBuilder()
            .WithTitle(
                metadata.Success
                    ? "Lua Success"
                    : "Lua Error"
            )
            .WithAuthor(Context.User)
            .WithCurrentTimestamp();

        if (metadata.Success)
            builder.WithColor(Color.Green);
        else
            builder.WithColor(Color.Red);

        var (fileNameOrOutput, outputFile) = DetermineDescription(
            metadata.Logs,
            Context.Interaction.Id.ToString() + "-output.txt"
        );

        if (outputFile == null && !string.IsNullOrEmpty(fileNameOrOutput))
            builder.WithDescription($"```\n{fileNameOrOutput}\n```");

        var (fileNameOrResult, resultFile) = DetermineResult(
            metadata.Success
                ? result
                : metadata.ErrorMessage,
            Context.Interaction.Id.ToString() + "-result.txt"
        );

        if (resultFile == null && !string.IsNullOrEmpty(fileNameOrResult))
            builder.AddField("Result", $"```\n{fileNameOrResult}\n```");

        builder.AddField("Execution Time", $"{metadata.ExecutionTime:f5}s");

        var attachments = new List<FileAttachment>();
        if (outputFile != null)
            attachments.Add(new(outputFile, fileNameOrOutput));

        if (resultFile != null)
            attachments.Add(new(resultFile, fileNameOrResult));

        var text = metadata.Success
                    ? string.IsNullOrEmpty(result)
                        ? "Executed script with no return!"
                        : null
                    : "An error occured while executing your script:";

        if (attachments.Count > 0)
            await FollowupWithFilesAsync(
                attachments,
                text,
                embed: builder.Build()
            );
        else
            await FollowupAsync(
                text,
                embed: builder.Build()
            );
    }

    private async Task<bool> ParseLuaAsync(string input)
    {
        var options = new LuaParseOptions(LuaSyntaxOptions.Roblox);
        var syntaxTree = LuaSyntaxTree.ParseText(input, options);

        var diagnostics = syntaxTree.GetDiagnostics();
        var errors = diagnostics.Where(diag => diag.Severity == DiagnosticSeverity.Error);

        if (errors.Any())
        {
            var errorString = string.Join("\n", errors.Select(err => err.ToString()));

            if (errorString.Length > _maxErrorLength)
            {
                var remaining = errorString.Length - _maxErrorLength;
                var remainingString = $"\n({remaining} characters remaining...)";

                errorString = string.Concat(errorString.AsSpan(0, _maxErrorLength - remainingString.Length), remainingString);
            }

            var embed = new EmbedBuilder()
                .WithTitle("Lua Error")
                .WithAuthor(Context.User)
                .WithCurrentTimestamp()
                .WithColor(Color.Red)
                .WithDescription($"```\n{errorString}\n```")
                .Build();

            await FollowupAsync("There was a Luau syntax error in your script:", embed: embed);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Execute a script via raw text.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    [SlashCommand("script", "Execute a script via raw text.")]
    public async Task ExecuteScriptFromTextAsync(
        [Summary("script", "The script to execute.")]
        string script
    )
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            await FollowupAsync("The script cannot be empty.");

            return;
        }

        script = GetCodeBlockContents(script);
        script = EscapeQuotes(script);

        var originalScript = script;

        await _scriptLogger.LogScriptAsync(script, Context);

        if (ContainsUnicode(script))
        {
            await FollowupAsync("The script cannot contain unicode characters as grid-servers cannot support unicode in transit.");

            return;
        }

        if (!await ParseLuaAsync(script))
            return;

        var scriptId = Guid.NewGuid().ToString();
        var filesafeScriptId = scriptId.Replace("-", "");
        var scriptName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? _gridServerFileHelper.GetGridServerScriptPath(filesafeScriptId)
            : Path.Combine(
                _gridSettings.GridServerSharedDirectoryInternalScripts,
                "scripts",
                filesafeScriptId + ".lua"
            );

        if (_scriptsSettings.LuaVMEnabled) // Disable if pre-luau, or wait for the file to be updated to support pre-luau
            script = string.Format(_luaUtility.LuaVMTemplate, script);

#if !PRE_JSON_EXECUTION
        // isAdmin allows a bypass of disabled methods and virtualized globals
        var settings = new ExecuteScriptSettings(filesafeScriptId, new Dictionary<string, object>() { { "is_admin", _adminUtility.UserIsAdmin(Context.User) } });
        var gserverCommand = new ExecuteScriptCommand(settings);
#else
        var gserverCommand = Lua.NewScript(
            scriptId,
            script,
            new Dictionary<string, object>() { { "is_admin", _adminUtility.UserIsAdmin(Context.User) } }
        );
#endif


        var gridJob = new GridJob() { id = scriptId, expirationInSeconds = _gridSettings.ScriptExecutionJobMaxTimeout.TotalSeconds };
        var job = new Job(Guid.NewGuid().ToString());

        var sw = Stopwatch.StartNew();

        try
        {
            var (soap, _, rejectionReason) = _jobManager.NewJob(job, _gridSettings.ScriptExecutionJobMaxTimeout.TotalSeconds, true);

            if (rejectionReason != null)
            {
                _logger.Error("The job was rejected: {0}", rejectionReason);

                await FollowupAsync("Internal error, please try again later.");

                return;
            }

            using (soap)
            {

#if !PRE_JSON_EXECUTION
                File.WriteAllText(scriptName, script, Encoding.ASCII);
#endif

                var serverResult = soap.BatchJobEx(gridJob, gserverCommand);

                Task.Run(() => _jobManager.CloseJob(job, true));

                var (newResult, metadata) = _luaUtility.ParseResult(serverResult);

                await HandleResponseAsync(newResult, metadata);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();

            Task.Run(() => _jobManager.CloseJob(job, false));

            if (ex is FaultException)
            {
                var message = ex.Message;
                if (GridSyntaxErrorRegex().IsMatch(message))
                {
                    var match = GridSyntaxErrorRegex().Match(message);
                    var line = match.Groups[1].Value;
                    var error = match.Groups[2].Value;

                    // We need to subtract the lines that the template adds (otherwise for one liners it will appear to be on line like 500 and something)
                    if (_scriptsSettings.LuaVMEnabled)
                    {
                        const string _marker = "{0}";

                        var template = _luaUtility.LuaVMTemplate;
                        var templateLines = template.Split('\n');

                        var lineIndex = Array.FindIndex(templateLines, line => line.StartsWith(_marker));

                        if (lineIndex != -1)
                            line = (int.Parse(line) - lineIndex).ToString();
                    }

                    await LuaErrorAsync($"Line {line}: {error}");

                    return;
                }

                if (message.Contains(_ErrorConvertingToJson))
                {
                    await LuaErrorAsync("The script returned a value that could not be converted to JSON.");

                    return;
                }
            }

            // If ex.InnerException.InnerException is a XmlException, it's likely that the script returned invalid ASCII characters.
            // Catch this and alert the user (only in the case of ex is CommunicationException, ex.InnerException is InvalidOperationException and ex.InnerException.InnerException is XmlException)
            if (ex is CommunicationException && ex.InnerException is InvalidOperationException && ex.InnerException.InnerException is XmlException)
            {
                await LuaErrorAsync("The script returned invalid ASCII characters.");

                return;
            }

            if (ex is TimeoutException)
            {
                await HandleResponseAsync(null, new() { ErrorMessage = "script exceeded timeout", ExecutionTime = sw.Elapsed.TotalSeconds, Success = false });

                return;
            }

            await AlertForSystem(script, originalScript, scriptId, scriptName, ex);

            throw;
        }
        finally
        {
            sw.Stop();

#if !PRE_JSON_EXECUTION
            try
            {
                _logger.Debug(
                    "Trying delete the script '{0}' at path '{1}'",
                    scriptId,
                    scriptName
                );
                scriptName.PollDeletion(
                    10,
                    ex => _logger.Warning("Failed to delete '{0}' because: {1}", scriptName, ex.Message),
                    () => _logger.Debug(
                        "Successfully deleted the script '{0}' at path '{1}'!",
                            scriptId,
                            scriptName
                        )
                );
            }
            catch (Exception ex)
            {
                _backtraceUtility.UploadException(ex);

                _logger.Warning(
                    "Failed to delete the user script '{0}' because '{1}'",
                    scriptName,
                    ex.Message
                );
            }
#endif
        }
    }

    private async Task AlertForSystem(string script, string originalScript, string scriptId, string scriptName, Exception ex)
    {
        // No alert for a timeout, as it is most likely just a user running an infinite loop.
        if (ex is TimeoutException)
            return;

        _backtraceUtility.UploadException(ex);

        var userInfo = Context.User.ToString();
        var guildInfo = Context.Guild?.ToString() ?? "DMs";
        var channelInfo = Context.Channel.ToString();

        // Script & original script in attachments
        var scriptAttachment = new FileAttachment(new MemoryStream(Encoding.ASCII.GetBytes(script)), "script.lua");
        var originalScriptAttachment = new FileAttachment(new MemoryStream(Encoding.ASCII.GetBytes(originalScript)), "original-script.lua");

        var content = $"""
                **User:** {userInfo}
                **Guild:** {guildInfo}
                **Channel:** {channelInfo}
                **Script ID:** {scriptId}
                **Script Name:** {scriptName}

                The script execution failed with the following error:
                ```{ex.Message}```
                """;

        await _discordWebhookAlertManager.SendAlertAsync(
            "Script Execution Fault",
            content,
            Color.Red,
            [scriptAttachment, originalScriptAttachment]
        );
    }

    /// <summary>
    /// Execute a script via a file.
    /// </summary>
    /// <param name="file">The file to execute.</param>
    [SlashCommand("file", "Execute a script via a file.")]
    public async Task ExecuteScriptFromFileAsync(
        [Summary("file", "The file to execute.")]
        IAttachment file
    )
    {
        if (!file.Filename.EndsWith(".lua"))
        {
            await FollowupAsync("The file must be a .lua file.");

            return;
        }

        var maxSize = _scriptsSettings.ScriptExecutionMaxFileSizeKb;

        if (file.Size / 1000 > maxSize)
        {
            await FollowupAsync($"The input attachment ({file.Filename}) cannot be larger than {maxSize} KiB!");

            return;
        }

        var contents = await file.GetAttachmentContentsAscii();

        await ExecuteScriptFromTextAsync(contents);
    }
}
