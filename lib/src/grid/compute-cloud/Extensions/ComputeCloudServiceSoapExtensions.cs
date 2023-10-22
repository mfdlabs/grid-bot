namespace Grid;

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Commands;
using ComputeCloud;

/// <summary>
/// Extension methods for <see cref="ComputeCloudServiceSoap"/>.
/// </summary>
public static class ComputeCloudServiceSoapExtensions
{
    private static readonly JsonSerializerSettings _JsonSerializerSettings = new JsonSerializerSettings
    {
        Converters = new JsonConverter[] { new StringEnumConverter() }
    };

    /// <inheritdoc cref="ComputeCloudServiceSoap.BatchJobEx(Job, ScriptExecution)"/>
    public static LuaValue[] BatchJobEx(this ComputeCloudServiceSoap service, Job job, GridCommand command)
    {
        var script = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return service.BatchJobEx(job, script);
    }

    /// <inheritdoc cref="ComputeCloudServiceSoap.BatchJobExAsync(Job, ScriptExecution)"/>
    public static async Task<LuaValue[]> BatchJobExAsync(this ComputeCloudServiceSoap service, Job job, GridCommand command)
    {
        var script = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return await service.BatchJobExAsync(job, script).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ComputeCloudServiceSoap.ExecuteEx(string, ScriptExecution)"/>
    public static LuaValue[] ExecuteEx(this ComputeCloudServiceSoap service, string script, GridCommand command)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return service.ExecuteEx(script, scriptExecution);
    }

    /// <inheritdoc cref="ComputeCloudServiceSoap.ExecuteExAsync(string, ScriptExecution)"/>
    public static async Task<LuaValue[]> ExecuteExAsync(this ComputeCloudServiceSoap service, string script, GridCommand command)
    {
        var scriptExecution = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return await service.ExecuteExAsync(script, scriptExecution).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ComputeCloudServiceSoap.OpenJobEx(Job, ScriptExecution)"/>
    public static LuaValue[] OpenJobEx(this ComputeCloudServiceSoap service, Job job, GridCommand command)
    {
        var script = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return service.OpenJobEx(job, script);
    }

    /// <inheritdoc cref="ComputeCloudServiceSoap.OpenJobExAsync(Job, ScriptExecution)"/>
    public static async Task<LuaValue[]> OpenJobExAsync(this ComputeCloudServiceSoap service, Job job, GridCommand command)
    {
        var script = Lua.NewScript(
            Guid.NewGuid().ToString(),
            JsonConvert.SerializeObject(command, Formatting.None, _JsonSerializerSettings)
        );

        return await service.OpenJobExAsync(job, script).ConfigureAwait(false);
    }
}
