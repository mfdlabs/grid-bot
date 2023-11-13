namespace Grid.Bot.Interactions.Private;

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
using Discord.WebSocket;
using Discord.Interactions;

using Extensions;

/// <summary>
/// Context for the C# script execution.
/// </summary>
public class CsharpExecutionContext
{
    /// <summary>
    /// The <see cref="ShardedInteractionContext"/>.
    /// </summary>
    public ShardedInteractionContext Context { get; init; }

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
/// Construct a new instance of <see cref="EvaluateCsharp"/>.
/// </summary>
/// <param name="scriptsSettings">The <see cref="ScriptsSettings"/>.</param>
/// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="services">The <see cref="IServiceProvider"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="scriptsSettings"/> cannot be null.
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="services"/> cannot be null.
/// </exception>
[Group("eval", "Commands used for evaluating C# code.")]
[RequireBotRole(BotRole.Owner)]
public partial class EvaluateCsharp(
    ScriptsSettings scriptsSettings,
    DiscordShardedClient client,
    IServiceProvider services
) : InteractionModuleBase<ShardedInteractionContext>
{
    private const int _maxErrorLength = EmbedBuilder.MaxDescriptionLength - 8;
    private const int _maxResultLength = EmbedFieldBuilder.MaxFieldValueLength - 8;

    private readonly ScriptsSettings _scriptsSettings = scriptsSettings ?? throw new ArgumentNullException(nameof(scriptsSettings));
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
            Context.Interaction.Id.ToString() + "-result.txt"
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

    /// <summary>
    /// Evaluates C# code.
    /// </summary>
    /// <param name="script">The code to evaluate.</param>
    [SlashCommand("script", "Evaluates C# code.")]
    public async Task EvaluateCodeFromTextAsync(
        [Summary("script", "The code to evaluate.")]
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
                        "Grid.Bot.Utility",
                        "Grid.Bot",
                        "Grid"
                    )
                    // Redirect stdout to our stream.
                    .WithAllowUnsafe(true),
                new CsharpExecutionContext
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

    /// <summary>
    /// Execute a script via a file.
    /// </summary>
    /// <param name="file">The file to execute.</param>
    [SlashCommand("file", "Execute a script via a file.")]
    public async Task EvaluateCodeFromFileAsync(
        [Summary("file", "The file to execute.")]
        IAttachment file
    )
    {
        if (!file.Filename.EndsWith(".cs"))
        {
            await FollowupAsync("The file must be a .cs file.");

            return;
        }

        var maxSize = _scriptsSettings.ScriptExecutionMaxFileSizeKb;

        if (file.Size / 1000 > maxSize)
        {
            await FollowupAsync($"The input attachment ({file.Filename}) cannot be larger than {maxSize} KiB!");

            return;
        }

        var contents = await file.GetAttachmentContentsAscii();

        await EvaluateCodeFromTextAsync(contents);
    }
}
