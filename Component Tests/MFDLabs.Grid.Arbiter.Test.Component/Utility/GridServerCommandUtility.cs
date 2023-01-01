namespace MFDLabs.Grid.Arbiter.Test.Component.Utility;

using System.Text;

using Networking;
using FileSystem;
using ComputeCloud;

public static class GridServerCommandUtility
{
    public static void ExecuteScript(this IGridServerInstance inst, string script)
    {
        var (ex, scriptName) = GenerateLuaScript(script);

        try
        {
            inst.BatchJobEx(new() { id = ex.name, category = 1, cores = 1.0, expirationInSeconds = int.MaxValue }, ex);
        }
        finally
        {
            scriptName.PollDeletion();
        }
    }

    public static void ExecuteScript(this IGridServerArbiter inst, string script)
    {
        var (ex, scriptName) = GenerateLuaScript(script);

        try
        {
            inst.BatchJobEx(new() { id = ex.name, category = 1, cores = 1.0, expirationInSeconds = int.MaxValue }, ex);
        }
        finally
        {
            scriptName.PollDeletion();
        }
    }

    public static async Task ExecuteScriptAsync(this IGridServerArbiter inst, string script)
    {
        var (ex, scriptName) = GenerateLuaScript(script);

        try
        {
            await inst.BatchJobAsync(new() { id = ex.name, category = 1, cores = 1.0, expirationInSeconds = int.MaxValue }, ex);
        }
        finally
        {
            scriptName.PollDeletion();
        }
    }

    public static (ScriptExecution, string) GenerateLuaScript(string contents)
    {
        var scriptId = NetworkingGlobal.GenerateUuidv4();
        var filesafeScriptId = scriptId.Replace("-", "");
        var scriptName = GridServerFileHelper.GetGridServerScriptPath(filesafeScriptId);

        var (command, _) = JsonScriptingUtility.GetSharedGameServerExecutionScript(
            filesafeScriptId
        );

        File.WriteAllText(scriptName, contents, Encoding.ASCII);

        return (Lua.NewScript(
            NetworkingGlobal.GenerateUuidv4(),
            command
        ), scriptName);
    }
}
