namespace Grid;

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Client;
using Commands;

/// <summary>
/// Extension methods for <see cref="GridServerServiceSoap"/>.
/// </summary>
public static class GridServerServiceSoapExtensions
{
    private static readonly JsonSerializerSettings _JsonSerializerSettings = new JsonSerializerSettings
    {
        Converters = new JsonConverter[] { new StringEnumConverter() }
    };

    /// <inheritdoc cref="GridServerServiceSoap.BatchJobEx(Job, ScriptExecution)"/>
    public static LuaValue[] BatchJobEx(this GridServerServiceSoap service, Job job, GridCommand command)
    {
        var script = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return service.BatchJobEx(job, script);
    }

    /// <inheritdoc cref="GridServerServiceSoap.BatchJobExAsync(Job, ScriptExecution)"/>
    public static async Task<LuaValue[]> BatchJobExAsync(this GridServerServiceSoap service, Job job, GridCommand command)
    {
        var script = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return await service.BatchJobExAsync(job, script).ConfigureAwait(false);
    }

    /// <inheritdoc cref="GridServerServiceSoap.ExecuteEx(string, ScriptExecution)"/>
    public static LuaValue[] ExecuteEx(this GridServerServiceSoap service, string script, GridCommand command)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return service.ExecuteEx(script, scriptExecution);
    }

    /// <inheritdoc cref="GridServerServiceSoap.ExecuteExAsync(string, ScriptExecution)"/>
    public static async Task<LuaValue[]> ExecuteExAsync(this GridServerServiceSoap service, string script, GridCommand command)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return await service.ExecuteExAsync(script, scriptExecution).ConfigureAwait(false);
    }

    /// <inheritdoc cref="GridServerServiceSoap.OpenJobEx(Job, ScriptExecution)"/>
    public static LuaValue[] OpenJobEx(this GridServerServiceSoap service, Job job, GridCommand command)
    {
        var script = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return service.OpenJobEx(job, script);
    }

    /// <inheritdoc cref="GridServerServiceSoap.OpenJobExAsync(Job, ScriptExecution)"/>
    public static async Task<LuaValue[]> OpenJobExAsync(this GridServerServiceSoap service, Job job, GridCommand command)
    {
        var script = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return await service.OpenJobExAsync(job, script).ConfigureAwait(false);
    }

    /// <inheritdoc cref="GridServerServiceSoap.BatchJobEx(Job, ScriptExecution)"/>
    public static LuaValue[] BatchJobEx(this GridServerServiceSoap service, Job job, string script)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            script
        );

        return service.BatchJobEx(job, scriptExecution);
    }

    /// <inheritdoc cref="GridServerServiceSoap.BatchJobExAsync(Job, ScriptExecution)"/>
    public static async Task<LuaValue[]> BatchJobExAsync(this GridServerServiceSoap service, Job job, string script)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            script
        );

        return await service.BatchJobExAsync(job, scriptExecution).ConfigureAwait(false);
    }

    /// <inheritdoc cref="GridServerServiceSoap.ExecuteEx(string, ScriptExecution)"/>
    public static LuaValue[] ExecuteEx(this GridServerServiceSoap service, string script, string command)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            command
        );

        return service.ExecuteEx(script, scriptExecution);
    }

    /// <inheritdoc cref="GridServerServiceSoap.ExecuteExAsync(string, ScriptExecution)"/>
    public static async Task<LuaValue[]> ExecuteExAsync(this GridServerServiceSoap service, string script, string command)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            command
        );

        return await service.ExecuteExAsync(script, scriptExecution).ConfigureAwait(false);
    }

    /// <inheritdoc cref="GridServerServiceSoap.OpenJobEx(Job, ScriptExecution)"/>
    public static LuaValue[] OpenJobEx(this GridServerServiceSoap service, Job job, string command)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            command
        );

        return service.OpenJobEx(job, scriptExecution);
    }

    /// <inheritdoc cref="GridServerServiceSoap.OpenJobExAsync(Job, ScriptExecution)"/>
    public static async Task<LuaValue[]> OpenJobExAsync(this GridServerServiceSoap service, Job job, string command)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            command
        );

        return await service.OpenJobExAsync(job, scriptExecution).ConfigureAwait(false);
    }
}
