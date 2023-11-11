namespace Grid.Bot.Interactions;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Collections.Generic;
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

using GridJob = ComputeCloud.Job;

/// <summary>
/// Interaction handler for executing Luau code.
/// </summary>
[Group("execute", "Commands used for executing Luau code.")]
public class ExecuteScript : InteractionModuleBase<ShardedInteractionContext>
{
    private const int _maxErrorLength = EmbedBuilder.MaxDescriptionLength - 8;
    private const int _maxResultLength = EmbedFieldBuilder.MaxFieldValueLength - 8;


    private readonly ILogger _logger;

    private readonly GridSettings _gridSettings;
    private readonly ScriptsSettings _scriptsSettings;

    private readonly ILuaUtility _luaUtility;
    private readonly IFloodCheckerRegistry _floodCheckerRegistry;
    private readonly IBacktraceUtility _backtraceUtility;
    private readonly IJobManager _jobManager;
    private readonly IAdminUtility _adminUtility;
    private readonly IDiscordWebhookAlertManager _discordWebhookAlertManager;

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
    /// </exception>
    public ExecuteScript(
        ILogger logger,
        GridSettings gridSettings,
        ScriptsSettings scriptsSettings,
        ILuaUtility luaUtility,
        IFloodCheckerRegistry floodCheckerRegistry,
        IBacktraceUtility backtraceUtility,
        IJobManager jobManager,
        IAdminUtility adminUtility,
        IDiscordWebhookAlertManager discordWebhookAlertManager
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _gridSettings = gridSettings ?? throw new ArgumentNullException(nameof(gridSettings));
        _scriptsSettings = scriptsSettings ?? throw new ArgumentNullException(nameof(scriptsSettings));
        _luaUtility = luaUtility ?? throw new ArgumentNullException(nameof(luaUtility));
        _floodCheckerRegistry = floodCheckerRegistry ?? throw new ArgumentNullException(nameof(floodCheckerRegistry));
        _backtraceUtility = backtraceUtility ?? throw new ArgumentNullException(nameof(backtraceUtility));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
        _discordWebhookAlertManager = discordWebhookAlertManager ?? throw new ArgumentNullException(nameof(discordWebhookAlertManager));
    }

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
                throw new ApplicationException("You are sending render commands too quickly, please wait a few moments and try again.");

            perUserFloodChecker.UpdateCount();
        }

        await base.BeforeExecuteAsync(command);
    }

    private static string GetCodeBlockContents(string s)
    {
        var match = Regex.Match(s, @"```(.*?)\s(.*?)```", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (match != null && match.Groups.Count == 3)
        {
            if (!s.Contains($"```{match.Groups[1].Value}\n"))
                return $"{match.Groups[1].Value} {match.Groups[2].Value}";

            return match.Groups[2].Value;
        }

        return s.Replace("`", ""); // Return the value here again?
    }

    private static string EscapeQuotes(string s) => Regex.Replace(s, "[\"“‘”]", "\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool ContainsUnicode(string s) => s.Any(c => c > 255);

    private (string, MemoryStream) DetermineDescription(string input, string fileName)
    {
        if (string.IsNullOrEmpty(input)) return (null, null);

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

        if (input.Length > _maxResultLength)
        {
            var maxSize = _scriptsSettings.ScriptExecutionMaxResultSizeKb;

            if (input.Length / 1000 > maxSize)
                return ($"The result cannot be larger than {maxSize} KiB", null);

            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        return (input, null);
    }

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
                var truncated = errorString.Substring(0, _maxErrorLength - 20);

                truncated += string.Format("({0} characters remaing...)", errorString.Length - (_maxErrorLength + 20));

                errorString = truncated;
            }

            var embed = new EmbedBuilder()
                .WithTitle("Luau Syntax Error")
                .WithAuthor(Context.User)
                .WithCurrentTimestamp()
                .WithColor(0xff, 0x00, 0x00)
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

        if (ContainsUnicode(script))
        {
            await FollowupAsync("The script cannot contain unicode characters as grid-servers cannot support unicode in transit.");

            return;
        }

        if (!await ParseLuaAsync(script))
            return;

        var scriptId = Guid.NewGuid().ToString();
        var filesafeScriptId = scriptId.Replace("-", "");
        var scriptName = Path.Combine(
            _gridSettings.GridServerSharedDirectoryInternalScripts,
            "scripts",
            filesafeScriptId + ".lua"
        );

        // isAdmin allows a bypass of disabled methods and virtualized globals
        var settings = new ExecuteScriptSettings(filesafeScriptId, new Dictionary<string, object>() { { "is_admin", _adminUtility.UserIsAdmin(Context.User) } });
        var gserverCommand = new ExecuteScriptCommand(settings);

        script = string.Format(_luaUtility.LuaVMTemplate, script);

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

                File.WriteAllText(scriptName, script, Encoding.ASCII);

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
                // Needs to be reported, get the original script, the fully constructed script and all information about channels, users, etc.
                await AlertForSystem(script, originalScript, scriptId, scriptName, ex);
                await FollowupAsync("There was an internal error, please try again later.");

                return;
            }

            if (ex is IOException)
            {
                _backtraceUtility.UploadCrashLog(ex);

                await FollowupAsync("There was an IO error when writing the script to the system, please try again later.");
            }

            if (ex is TimeoutException)
            {
                await AlertForSystem(script, originalScript, scriptId, scriptName, ex);
                await HandleResponseAsync(null, new() { ErrorMessage = "script exceeded timeout", ExecutionTime = sw.Elapsed.TotalSeconds, Success = false });

                return;
            }

            if (ex is not IOException) throw;
        }
        finally
        {
            sw.Stop();

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
                _backtraceUtility.UploadCrashLog(ex);

                _logger.Warning(
                    "Failed to delete the user script '{0}' because '{1}'",
                    scriptName,
                    ex.Message
                );
            }
        }
    }

    private async Task AlertForSystem(string script, string originalScript, string scriptId, string scriptName, Exception ex)
    {
        _backtraceUtility.UploadCrashLog(ex);

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
            new[] { scriptAttachment, originalScriptAttachment }
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
