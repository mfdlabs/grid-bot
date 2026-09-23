namespace Grid.Bot.UnifiedCommands.Public;

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
using Discord.Commands;
using Discord.Interactions;

using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;

using Logging;
using FileSystem;

using Utility;
using Commands;
using Extensions;

using Grid.Commands;
using Grid.ProcessManagement;
using Grid.ProcessManagement.Core;

using ClientJob = Client.Job;

using TextCommandGroup = Discord.Commands.GroupAttribute;
using TextCommandSummary = Discord.Commands.SummaryAttribute;
using InteractionGroup = Discord.Interactions.GroupAttribute;
using InteractionSummary = Discord.Interactions.SummaryAttribute;

using TextCommandModuleBase = Discord.Commands.ModuleBase;
using InteractionModuleBase = Discord.Interactions.InteractionModuleBase;

/// <summary>
/// Construct a new instance of <see cref="ExecuteScript"/>.
/// </summary>
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
)
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
            ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithSyntaxErrors.WithLabels("grid-server-syntax-error:metadata").Inc();

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
            {
                ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithResultsExceedingMaxSize.WithLabels(input.Length.ToString()).Inc();

                return ($"The output cannot be larger than {maxSize} KiB", null);
            }

            ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithResultsViaFiles.Inc();

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
            {
                ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithResultsExceedingMaxSize.WithLabels(input.Length.ToString()).Inc();

                return ($"The result cannot be larger than {maxSize} KiB", null);
            }

            ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithResultsViaFiles.Inc();

            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        return (input, null);
    }

    private async Task LuaErrorAsync(IUnifiedCommandContext context, string error)
        => await HandleResponseAsync(context, null, new() { ErrorMessage = error, ExecutionTime = 0, Success = false });

    private async Task HandleResponseAsync(IUnifiedCommandContext context, string result, ReturnMetadata metadata)
    {
        var builder = new EmbedBuilder()
            .WithTitle(
                metadata.Success
                    ? "Lua Success"
                    : "Lua Error"
            )
            .WithAuthor(context.User)
            .WithCurrentTimestamp();

        if (metadata.Success)
        {
            ScriptExecutionPerformanceCounters.TotalSuccessfulScriptExecutions.Inc();

            builder.WithColor(Color.Green);
        }
        else
        {
            ScriptExecutionPerformanceCounters.TotalFailedScriptExecutionsDueToLuaError.Inc();

            builder.WithColor(Color.Red);
        }

        var id = context.Message?.Id.ToString() ?? context.Interaction?.Id.ToString();

        var (fileNameOrOutput, outputFile) = DetermineDescription(
            metadata.Logs,
            id + "-output.txt"
        );

        if (outputFile == null && !string.IsNullOrEmpty(fileNameOrOutput))
            builder.WithDescription($"```\n{fileNameOrOutput}\n```");

        var (fileNameOrResult, resultFile) = DetermineResult(
            metadata.Success
                ? result
                : metadata.ErrorMessage,
            id + "-result.txt"
        );

        if (resultFile == null && !string.IsNullOrEmpty(fileNameOrResult))
            builder.AddField("Result", $"```\n{fileNameOrResult}\n```");

        builder.AddField("Execution Time", $"{metadata.ExecutionTime:f5}s");

        ScriptExecutionPerformanceCounters.ScriptExecutionAverageExecutionTime.Observe(metadata.ExecutionTime);

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
            await context.RespondWithFilesAsync(
                attachments,
                text,
                embed: builder.Build()
            );
        else
            await context.RespondAsync(
                text,
                embed: builder.Build()
            );
    }

    private static async Task<bool> ParseLuaAsync(IUnifiedCommandContext context, string input)
    {
        var options = new LuaParseOptions(LuaSyntaxOptions.Roblox);
        var syntaxTree = LuaSyntaxTree.ParseText(input, options);

        var diagnostics = syntaxTree.GetDiagnostics();
        var errors = diagnostics.Where(diag => diag.Severity == DiagnosticSeverity.Error);

        if (errors.Any())
        {
            ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithSyntaxErrors.WithLabels("pre-parser-syntax-error").Inc();

            var errorString = string.Join("\n", errors.Select(err => err.ToString()));

            if (errorString.Length > _maxErrorLength)
            {
                var remaining = errorString.Length - _maxErrorLength;
                var remainingString = $"\n({remaining} characters remaining...)";

                errorString = string.Concat(errorString.AsSpan(0, _maxErrorLength - remainingString.Length), remainingString);
            }

            var embed = new EmbedBuilder()
                .WithTitle("Lua Error")
                .WithAuthor(context.User)
                .WithCurrentTimestamp()
                .WithColor(Color.Red)
                .WithDescription($"```\n{errorString}\n```")
                .Build();

            await context.RespondAsync("There was a Luau syntax error in your script:", embed: embed).ConfigureAwait(false);

            return false;
        }

        return true;
    }

    private async Task AlertForSystem(IUnifiedCommandContext context, string script, string originalScript, string scriptId, string scriptName, Exception ex)
    {
        _backtraceUtility.UploadException(ex);

        var userInfo = context.User.ToString();
        var guildInfo = context.Guild?.ToString() ?? "DMs";
        var channelInfo = context.Channel?.ToString();

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
    /// Executes before the executeScript command is processed, checking for admin status and flood control.
    /// </summary>
    /// <param name="context">The context of the unified command.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ApplicationException">
    /// - Thrown when the user is blocked by the global flood checker.
    /// - Thrown when the user is blocked by the per-user flood checker.
    /// </exception>
    public Task BeforeExecuteAsync(IUnifiedCommandContext context)
    {
        if (!_adminUtility.UserIsAdmin(context.User))
        {
            if (_floodCheckerRegistry.ScriptExecutionFloodChecker.IsFlooded())
            {
                ScriptExecutionPerformanceCounters.TotalScriptExecutionsBlockedByGlobalFloodChecker.Inc();

                return Task.FromException(new ApplicationException("Too many people are using this command at once, please wait a few moments and try again."));
            }

            _floodCheckerRegistry.ScriptExecutionFloodChecker.UpdateCount();

            var perUserFloodChecker = _floodCheckerRegistry.GetPerUserScriptExecutionFloodChecker(context.User.Id);
            if (perUserFloodChecker.IsFlooded())
            {
                ScriptExecutionPerformanceCounters.TotalScriptExecutionsBlockedByPerUserFloodChecker.WithLabels(context.User.Id.ToString()).Inc();

                return Task.FromException(new ApplicationException("You are sending execute commands too quickly, please wait a few moments and try again."));
            }

            perUserFloodChecker.UpdateCount();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Execute a script via raw text.
    /// </summary>
    /// <param name="context">The unified command context.</param>
    /// <param name="file">The file attachment containing the script, if any.</param>
    /// <param name="script">The script to execute.</param>
    public async Task DoExecuteScriptAsync(IUnifiedCommandContext context, IAttachment file = null, string script = "")
    {
        ScriptExecutionPerformanceCounters.TotalScriptExecutionsByUser.WithLabels(context.User.Id.ToString()).Inc();

        if (string.IsNullOrWhiteSpace(script))
        {
            if (file is null)
            {
                await context.RespondAsync("The command must include text or a file attachment!").ConfigureAwait(false);

                return;
            }

            if (!file.Filename.EndsWith(".lua"))
            {
                await context.RespondAsync("The file must be a .lua file.").ConfigureAwait(false);

                return;
            }

            var maxSize = _scriptsSettings.ScriptExecutionMaxFileSizeKb;

            if (file.Size / 1000 > maxSize)
            {
                await context.RespondAsync($"The input attachment ({file.Filename}) cannot be larger than {maxSize} KiB!").ConfigureAwait(false);

                return;
            }

            ScriptExecutionPerformanceCounters.TotalScriptExecutionsFromFiles.WithLabels(file.Filename, file.Size.ToString()).Inc();

            script = await file.GetAttachmentContentsAscii();
        }

        script = GetCodeBlockContents(script);

        if (string.IsNullOrEmpty(script))
        {
            ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithNoContent.Inc();

            await LuaErrorAsync(context, "There must be content within a code block!");

            return;
        }

        script = EscapeQuotes(script);

        var originalScript = script;

        await _scriptLogger.LogScriptAsync(script, context);

        if (ContainsUnicode(script))
        {
            ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithUnicode.Inc();

            await LuaErrorAsync(context, "Scripts can only contain ASCII characters!");

            return;
        }

        if (!await ParseLuaAsync(context, script))
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
        {
            ScriptExecutionPerformanceCounters.TotalScriptExecutionsUsingLuaVM.Inc();

            script = string.Format(_luaUtility.LuaVMTemplate, script);
        }

#if !PRE_JSON_EXECUTION
        // isAdmin allows a bypass of disabled methods and virtualized globals
        var settings = new ExecuteScriptSettings(filesafeScriptId, new Dictionary<string, object>() { { "is_admin", _adminUtility.UserIsAdmin(context.User) } });
        var gserverCommand = new ExecuteScriptCommand(settings);
#else
        var gserverCommand = Lua.NewScript(
            scriptId,
            script,
            new Dictionary<string, object>() { { "is_admin", _adminUtility.UserIsAdmin(Context.User) } }
        );
#endif


        var gridJob = new ClientJob() { id = scriptId, expirationInSeconds = _gridSettings.ScriptExecutionJobMaxTimeout.TotalSeconds };
        var job = new Job(Guid.NewGuid().ToString());

        var sw = Stopwatch.StartNew();

        try
        {
            var (soap, _, rejectionReason) = _jobManager.NewJob(job, _gridSettings.ScriptExecutionJobMaxTimeout.TotalSeconds, true);

            if (rejectionReason != null)
            {
                _logger.Error("The job was rejected: {0}", rejectionReason);

                await context.RespondAsync("Internal error, please try again later.").ConfigureAwait(false);

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

                await HandleResponseAsync(context, newResult, metadata);
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
                    ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithSyntaxErrors.WithLabels("grid-server-syntax-error:fault").Inc();

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

                    await LuaErrorAsync(context, $"Line {line}: {error}");

                    return;
                }

                if (message.Contains(_ErrorConvertingToJson))
                {
                    ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithNonJsonSerializableResults.Inc();

                    await LuaErrorAsync(context, "The script returned a value that could not be converted to JSON.");

                    return;
                }
            }

            // If ex.InnerException.InnerException is a XmlException, it's likely that the script returned invalid ASCII characters.
            // Catch this and alert the user (only in the case of ex is CommunicationException, ex.InnerException is InvalidOperationException and ex.InnerException.InnerException is XmlException)
            if (ex is CommunicationException && ex.InnerException is InvalidOperationException && ex.InnerException.InnerException is XmlException)
            {
                ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithNonAsciiResults.Inc();

                await LuaErrorAsync(context, "The script returned invalid ASCII characters.");

                return;
            }

            if (ex is TimeoutException)
            {
                ScriptExecutionPerformanceCounters.TotalScriptExecutionsThatTimedOut.Inc();

                await HandleResponseAsync(context, null, new() { ErrorMessage = "script exceeded timeout", ExecutionTime = sw.Elapsed.TotalSeconds, Success = false });

                return;
            }

            ScriptExecutionPerformanceCounters.TotalScriptExecutionsWithUnexpectedExceptions.WithLabels(ex.GetType().ToString()).Inc();

            if (ex is not Discord.Net.HttpException)
                await AlertForSystem(context, script, originalScript, scriptId, scriptName, ex);

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
                    onFailure: ex => _logger.Warning("Failed to delete '{0}' because: {1}", scriptName, ex.Message),
                    onSuccess: () => _logger.Debug(
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
}

#region MODULE PROXIES

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ExecuteScriptTextCommand(ExecuteScript executeScriptCommand) : TextCommandModuleBase
{
    private readonly ExecuteScript _executeScriptCommand = executeScriptCommand ?? throw new ArgumentNullException(nameof(executeScriptCommand));

    protected override async Task BeforeExecuteAsync(CommandInfo command)
        => await _executeScriptCommand.BeforeExecuteAsync(new UnifiedCommandContext(Context)).ConfigureAwait(false);


    [Command("execute"), TextCommandSummary("Execute a script via raw text."), Alias("ex", "exc", "x")]
    public async Task DoExecuteScriptAsync([Remainder] string script = "")
        => await _executeScriptCommand.DoExecuteScriptAsync(new UnifiedCommandContext(Context), Context.GetAttachment(), script).ConfigureAwait(false);
}


[InteractionGroup("execute", "Commands used for executing Luau code.")]
[IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
public class ExecuteScriptInteraction(ExecuteScript executeScriptCommand) : InteractionModuleBase
{
    private readonly ExecuteScript _executeScriptCommand = executeScriptCommand ?? throw new ArgumentNullException(nameof(executeScriptCommand));

    public override async Task BeforeExecuteAsync(ICommandInfo command)
        => await _executeScriptCommand.BeforeExecuteAsync(new UnifiedCommandContext(Context)).ConfigureAwait(false);

    [SlashCommand("script", "Execute a script via raw text.")]
    public async Task ExecuteScriptFromTextAsync(
        [InteractionSummary("script", "The script to execute.")]
        string script
    ) => await _executeScriptCommand.DoExecuteScriptAsync(new UnifiedCommandContext(Context), null, script).ConfigureAwait(false);

    [SlashCommand("file", "Execute a script via a file.")]
    public async Task ExecuteScriptFromFileAsync(
        [InteractionSummary("file", "The file to execute.")]
        IAttachment file
    ) => await _executeScriptCommand.DoExecuteScriptAsync(new UnifiedCommandContext(Context), file).ConfigureAwait(false);
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#endregion