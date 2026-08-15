namespace Grid.Bot.Commands.Private;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Utility;
using Extensions;

/// <summary>
/// Context for the C# script execution.
/// </summary>
public class CSharpExecutionContext
{
    /// <summary>
    /// The <see cref="ShardedCommandContext"/>.
    /// </summary>
    public ShardedCommandContext Context { get; init; }

    /// <summary>
    /// The <see cref="DiscordShardedClient"/>.
    /// </summary>
    public DiscordShardedClient Client { get; init; }

    /// <summary>
    /// The <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider Services { get; init; }
}

/// <summary>
/// Interaction handler for evaluating C# code.
/// </summary>
/// <summary>
/// Construct a new instance of <see cref="EvaluateCSharp"/>.
/// </summary>
/// <param name="scriptsSettings">The <see cref="ScriptsSettings"/>.</param>
/// <param name="commandsSettings">The <see cref="CommandsSettings"/>.</param>
/// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="services">The <see cref="IServiceProvider"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="scriptsSettings"/> cannot be null.
/// - <paramref name="commandsSettings"/> cannot be null.
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="services"/> cannot be null.
/// </exception>
[LockDownCommand(BotRole.Owner)]
[RequireBotRole(BotRole.Owner)]
public partial class EvaluateCSharp(
    ScriptsSettings scriptsSettings,
    CommandsSettings commandsSettings,
    DiscordShardedClient client,
    IServiceProvider services
) : ModuleBase<ShardedCommandContext>
{
    private const int _maxErrorLength = EmbedBuilder.MaxDescriptionLength - 8;
    private const int _maxResultLength = EmbedFieldBuilder.MaxFieldValueLength - 8;

    private readonly ScriptsSettings _scriptsSettings = scriptsSettings ?? throw new ArgumentNullException(nameof(scriptsSettings));
    private readonly CommandsSettings _commandsSettings = commandsSettings ?? throw new ArgumentNullException(nameof(commandsSettings));
    private readonly DiscordShardedClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));
    
    [GeneratedRegex(@"```(.*?)\s(.*?)```", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();
    [GeneratedRegex("[\"“‘”]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex QuotesRegex();

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

        if (input.Length > _maxErrorLength)
            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));

        return (input, null);
    }

    private (string, MemoryStream) DetermineResult(string input, string fileName)
    {
        if (string.IsNullOrEmpty(input)) return (null, null);

        if (input.Length > _maxResultLength)
            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));

        return (input, null);
    }

    private async Task HandleResponseAsync(string result, ScriptState metadata, Stopwatch timing)
    {
        var builder = new EmbedBuilder()
            .WithTitle(
                metadata?.Exception == null && metadata != null
                    ? "C# Success"
                    : "C# Error"
            )
            .WithAuthor(Context.User)
            .WithCurrentTimestamp();

        if (metadata?.Exception == null && metadata != null)
            builder.WithColor(Color.Green);
        else
            builder.WithColor(Color.Red);

        var (fileNameOrResult, resultFile) = DetermineResult(
            metadata?.Exception == null
                ? result
                : metadata.Exception.ToString(),
            Context.Message.Id.ToString() + "-result.txt"
        );

        if (resultFile == null && !string.IsNullOrEmpty(fileNameOrResult))
            builder.AddField("Result", $"```\n{fileNameOrResult}\n```");

        builder.AddField("Execution Time", $"{timing.Elapsed.TotalSeconds:f5}s");

        var attachments = new List<FileAttachment>();
        if (resultFile != null)
            attachments.Add(new(resultFile, fileNameOrResult));

        var text = metadata?.Exception == null && metadata != null
            ? string.IsNullOrEmpty(result)
                ? "Executed script with no return!"
                : null
            : "An error occured while executing your script:";

        if (attachments.Count > 0)
            await this.ReplyWithFilesAsync(
                attachments,
                text,
                embed: builder.Build()
            );
        else
            await this.ReplyWithReferenceAsync(
                text,
                embed: builder.Build()
            );
    }

    /// <inheritdoc cref="ModuleBase{TContext}.BeforeExecuteAsync(CommandInfo)"/>
    protected override async Task BeforeExecuteAsync(CommandInfo command)
    {
        if (!_commandsSettings.EvaluateCSharpCommandEnabled)
            throw new ApplicationException(
                $"The EvaluateCSharp command is currently disabled. Please enable via {nameof(CommandsSettings.EvaluateCSharpCommandEnabled)}."
            );

        await base.BeforeExecuteAsync(command);
    }

    /// <summary>
    /// Evaluates C# code.
    /// </summary>
    /// <param name="script">The code to evaluate.</param>
    [Command("eval"), Summary("Execute a script via raw text.")]
    public async Task EvaluateCodeFromTextAsync([Remainder] string script = "")
    {
        using var _ = Context.Channel.EnterTypingState();

        if (string.IsNullOrWhiteSpace(script))
        {
            var file = Context.Message.Attachments.FirstOrDefault();
            if (file is null)
            {
                await this.ReplyWithReferenceAsync("The command must include text or a file attachment!");

                return;
            }

            if (!file.Filename.EndsWith(".lua"))
            {
                await this.ReplyWithReferenceAsync("The file must be a .lua file.");

                return;
            }

            var maxSize = _scriptsSettings.ScriptExecutionMaxFileSizeKb;

            if (file.Size / 1000 > maxSize)
            {
                await this.ReplyWithReferenceAsync($"The input attachment ({file.Filename}) cannot be larger than {maxSize} KiB!");

                return;
            }

            ScriptExecutionPerformanceCounters.TotalScriptExecutionsFromFiles.WithLabels(file.Filename, file.Size.ToString()).Inc();

            script = await file.GetAttachmentContentsAscii();
        }

        script = GetCodeBlockContents(script);
        script = EscapeQuotes(script);

        var timing = Stopwatch.StartNew();

        try
        {
            var result = await CSharpScript.RunAsync(
                script,
                ScriptOptions.Default
                    .WithReferences(
                        Assembly.GetEntryAssembly(),
                        Assembly.GetExecutingAssembly()
                    )
                    .WithImports(
                        "System",
                        "System.Linq",
                        "System.Collections.Generic",
                        "System.Threading.Tasks",

                        "Discord",
                        "Discord.WebSocket",
                        "Discord.Interactions",

                        "Grid",
                        "Grid.Bot",
                        "Grid.Bot.Utility",
                        "Grid.Bot.Extensions"
                    )
                    .WithAllowUnsafe(true),
                new CSharpExecutionContext
                {
                    Context = Context,
                    Client = _client,
                    Services = _services
                }
            );

            timing.Stop();

            await HandleResponseAsync(result.ReturnValue?.ToString(), result, timing);
        }
        catch (CompilationErrorException ex)
        {
            timing.Stop();

            await HandleResponseAsync(ex.Message, null, timing);
        }
        catch (Exception ex)
        {
            timing.Stop();

            await HandleResponseAsync(ex.ToString(), null, timing);
        }
        finally
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }
    }
}