namespace Grid.Bot.Commands.Private;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Discord;
using Discord.Commands;

using Newtonsoft.Json;

using FileSystem;

using Utility;
using Extensions;

using Eval.Runner.Models;

/// <summary>
/// Interaction handler for evaluating C# code.
/// </summary>
/// <summary>
/// Construct a new instance of <see cref="EvaluateCSharp"/>.
/// </summary>
/// <param name="scriptsSettings">The <see cref="ScriptsSettings"/>.</param>
/// <param name="commandsSettings">The <see cref="CommandsSettings"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="scriptsSettings"/> cannot be null.
/// - <paramref name="commandsSettings"/> cannot be null.
/// </exception>
[LockDownCommand(BotRole.Owner)]
[RequireBotRole(BotRole.Owner)]
public partial class EvaluateCSharp(
    ScriptsSettings scriptsSettings,
    CommandsSettings commandsSettings
) : ModuleBase<ShardedCommandContext>
{
    private const int _maxErrorLength = EmbedBuilder.MaxDescriptionLength - 8;
    private const int _maxResultLength = EmbedFieldBuilder.MaxFieldValueLength - 8;

    private readonly ScriptsSettings _scriptsSettings = scriptsSettings ?? throw new ArgumentNullException(nameof(scriptsSettings));
    private readonly CommandsSettings _commandsSettings = commandsSettings ?? throw new ArgumentNullException(nameof(commandsSettings));

    private static readonly TimeSpan _scriptExecutionTimeout = TimeSpan.FromSeconds(10);

    private static readonly Assembly _evalRunnerAssembly = typeof(ResultModel).Assembly;
    private static readonly string _evalRunnerProgram = _evalRunnerAssembly.GetName().Name;
    private static readonly string _evalRunnerFullPath =
        Path.Combine(
            Path.GetDirectoryName(_evalRunnerAssembly.Location),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? _evalRunnerProgram + ".exe"
                : _evalRunnerProgram
        );

    static EvaluateCSharp()
    {
        if (!File.Exists(_evalRunnerFullPath))
            throw new FileNotFoundException($"The eval-runner executable was not found at {_evalRunnerFullPath}. Please ensure that the eval-runner is built and present in the same directory as the bot executable.");
    }

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

    private async Task CSharpErrorAsync(string error, TimeSpan elapsed)
        => await HandleResponseAsync(null, new() { ErrorMessage = error, ExecutionTime = elapsed.TotalSeconds, Success = false });

    private async Task HandleResponseAsync(string result, EvalMetadata metadata)
    {
        var builder = new EmbedBuilder()
            .WithTitle(
                metadata.Success
                    ? "C# Success"
                    : "C# Error"
            )
            .WithAuthor(Context.User)
            .WithCurrentTimestamp();

        if (metadata.Success)
            builder.WithColor(Color.Green);
        else
            builder.WithColor(Color.Red);

        var (fileNameOrStdout, stdoutFile) = DetermineDescription(
            metadata.StdoutLogs,
            Context.Message.Id.ToString() + "-stdout.txt"
        );

        if (stdoutFile == null && !string.IsNullOrEmpty(fileNameOrStdout))
            builder.AddField("STDOUT", $"```\n{fileNameOrStdout}\n```");

        var (fileNameOrStderr, stderrFile) = DetermineDescription(
            metadata.StderrLogs,
            Context.Message.Id.ToString() + "-stderr.txt"
        );

        if (stderrFile == null && !string.IsNullOrEmpty(fileNameOrStderr))
            builder.AddField("STDERR", $"```\n{fileNameOrStderr}\n```");

        var (fileNameOrResult, resultFile) = DetermineResult(
            metadata.Success
                ? result
                : metadata.ErrorMessage,
            Context.Message.Id.ToString() + "-result.txt"
        );

        if (resultFile == null && !string.IsNullOrEmpty(fileNameOrResult))
            builder.AddField("Result", $"```\n{fileNameOrResult}\n```");

        builder.AddField("Execution Time", $"{metadata.ExecutionTime:f5}s");

        var attachments = new List<FileAttachment>();
        if (stdoutFile != null)
            attachments.Add(new(stdoutFile, fileNameOrStdout));

        if (resultFile != null)
            attachments.Add(new(resultFile, fileNameOrResult));

        var text = metadata.Success
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

        var tempScriptFileName = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempScriptFileName, script);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _evalRunnerFullPath,
                Arguments = $"\"{tempScriptFileName}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // Pass-through environment variables to the eval-runner process
            foreach (var envVar in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>())
                processStartInfo.Environment[envVar.Key.ToString()] = envVar.Value.ToString();

            // Only log line present will be the result of the script execution, which is a JSON string.
            var process = Process.Start(processStartInfo) 
                ?? throw new InvalidOperationException("Failed to start the eval-runner process.");

            using var cancelTokenSrc = new CancellationTokenSource(_scriptExecutionTimeout);

            await process.WaitForExitAsync(cancelTokenSrc.Token);

            var output = await process.StandardOutput.ReadToEndAsync(cancelTokenSrc.Token);
            var result = JsonConvert.DeserializeObject<ResultModel>(output)
                ?? throw new InvalidOperationException("Failed to deserialize the output from the eval-runner process.");

            timing.Stop();

            await HandleResponseAsync(result.Result, result.Metadata);
        }
        catch (OperationCanceledException)
        {
            timing.Stop();

            await CSharpErrorAsync($"The script execution exceeded the timeout of {_scriptExecutionTimeout.TotalSeconds} seconds.", timing.Elapsed);
        }
        catch (InvalidOperationException ex)
        {
            timing.Stop();

            await CSharpErrorAsync($"An error occurred while executing the script: {ex.Message}", timing.Elapsed);
        }
        catch (Exception ex)
        {
            timing.Stop();

            await CSharpErrorAsync(ex.ToString(), timing.Elapsed);
        }
        finally
        {
            if (timing.IsRunning) timing.Stop();

            tempScriptFileName.PollDeletion();
        }
    }
}
