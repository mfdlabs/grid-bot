namespace Grid.Bot.Eval.Runner;

using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

using Newtonsoft.Json;

using Logging;
using Threading.Extensions;

using Models;

internal class Program
{
    private static readonly Assembly _utilityAssembly = Assembly.Load("Shared.Utility");
    private static readonly Assembly _settingsAssembly = Assembly.Load("Shared.Settings");
    private static readonly Assembly _discordNetRest = Assembly.Load("Discord.Net.Rest");

    private static readonly ScriptOptions _ScriptOptions = 
        ScriptOptions.Default
            .WithReferences(
                _utilityAssembly,
                _settingsAssembly,
                _discordNetRest
            )
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Threading.Tasks",

                "Discord",
                "Discord.Rest",

                "Grid",
                "Grid.Bot",
                "Grid.Bot.Utility",
                "Grid.Bot.Extensions"
            )
            .WithAllowUnsafe(true);

    static void WriteResultToStdout(string result, EvalMetadata metadata)
    {
        var resultModel = new ResultModel
        {
            Result = result,
            Metadata = metadata
        };

        var jsonResult = JsonConvert.SerializeObject(resultModel);
        Console.Out.WriteLine(jsonResult);
    }

    static void WriteResult(string result, TextWriter stdout, TextWriter stderr, TimeSpan timing)
    {
        var metadata = new EvalMetadata
        {
            Success = true,
            ExecutionTime = timing.TotalSeconds,
            ErrorMessage = null,
            StdoutLogs = stdout.ToString(),
            StderrLogs = stderr.ToString()
        };

        WriteResultToStdout(result, metadata);
    }

    static void WriteError(string error, TimeSpan timing)
    {
        var resultModel = new ResultModel
        {
            Result = "",
            Metadata = new EvalMetadata
            {
                ErrorMessage = error,
                ExecutionTime = timing.TotalSeconds,
                Success = false
            }
        };

        var jsonResult = JsonConvert.SerializeObject(resultModel);
        Console.Out.WriteLine(jsonResult);
    }

    private static TextWriter _StdoutWriter;
    private static TextWriter _StderrWriter;

    private static void OverrideConsoleOutput(TextWriter stdout, TextWriter stderr)
    {
        _StdoutWriter = Console.Out;
        _StderrWriter = Console.Error;

        Console.SetOut(stdout);
        Console.SetError(stderr);
    }

    private static void RestoreConsoleOutput()
    {
        if (_StdoutWriter != null)
        {
            Console.SetOut(_StdoutWriter);
            _StdoutWriter = null;
        }

        if (_StderrWriter != null)
        {
            Console.SetError(_StderrWriter);
            _StderrWriter = null;
        }
    }

    private static void Main(string[] args)
    {
        Logger.ConcurrentLoggingEnabled = false; // Disable concurrent logging for this process to avoid issues with redirected output
        Logger.LogPrefixesEnabled = false;

        var scriptFilePath = args[0];

        var timing = Stopwatch.StartNew();

        if (!File.Exists(scriptFilePath))
        {
            WriteError($"Script file not found: {scriptFilePath}", timing.Elapsed);

            return;
        }

        // Redirect stdout and stderr after here
        // to memory
        var stdoutWriter = new StringWriter();
        var stderrWriter = new StringWriter();

        OverrideConsoleOutput(stdoutWriter, stderrWriter);

        try
        {
            var script = File.ReadAllText(scriptFilePath);

            var csharpScript = CSharpScript.Create(script, _ScriptOptions);
            var runner = csharpScript.CreateDelegate();

            timing.Restart();

            var result = runner().Sync();

            timing.Stop();

            RestoreConsoleOutput();

            WriteResult(result?.ToString() ?? "", stdoutWriter, stderrWriter, timing.Elapsed);
        }
        catch (CompilationErrorException ex)
        {
            timing.Stop();
            RestoreConsoleOutput();

            WriteError($"Compilation error: {string.Join(Environment.NewLine, ex.Diagnostics)}", timing.Elapsed);
        }
        catch (AggregateException ex) when (ex.InnerException is not null)
        {
            timing.Stop();
            RestoreConsoleOutput();

            WriteError(ex.InnerException.Message, timing.Elapsed);
        }
        catch (IOException ex)
        {
            timing.Stop();
            RestoreConsoleOutput();

            WriteError($"IO error: {ex.Message}", timing.Elapsed);
        }
        catch (Exception ex)
        {
            timing.Stop();
            RestoreConsoleOutput();

            WriteError($"Unexpected error: {ex.Message}", timing.Elapsed);
        }
    }
}