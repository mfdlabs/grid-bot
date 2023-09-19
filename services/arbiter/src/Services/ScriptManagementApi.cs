namespace Grid.Arbiter.Service;

using System;
using System.IO;
using System.Threading.Tasks;

using Grpc.Core;

using V1;
using FileSystem;
using Text.Extensions;

/// <summary>
/// Implementation for <see cref="ScriptManagementAPI.ScriptManagementAPIBase"/>
/// </summary>
public class ScriptManagementApi : ScriptManagementAPI.ScriptManagementAPIBase
{
    /// <inheritdoc cref="ScriptManagementAPI.ScriptManagementAPIBase.WriteScript(WriteScriptRequest, ServerCallContext)"/>
    public override Task<WriteScriptResponse> WriteScript(WriteScriptRequest request, ServerCallContext context)
    {
        if (request.Name.IsNullOrEmpty())
            return Task.FromResult(
                new WriteScriptResponse
                {
                    ScriptName = request.Name,
                    ScriptType = request.Type,
                    Success = false,
                    ErrorMessage = "The parameter [name] is required!"
                }
            );

        try
        {
            var scriptPath = GridServerFileHelper.GetGridServerScriptPath(
                request.Name,
                (Grid.ScriptType)request.Type
            );

            File.WriteAllBytes(scriptPath, request.Content.ToByteArray());

            return Task.FromResult(
                new WriteScriptResponse
                {
                    ScriptName = request.Name,
                    ScriptType = request.Type,
                    Success = true,
                    ScriptPath = scriptPath
                }
            );
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                new WriteScriptResponse
                {
                    ScriptName = request.Name,
                    ScriptType = request.Type,
                    Success = false,
                    ErrorMessage = ex.Message
                }
            );
        }
    }

    /// <inheritdoc cref="ScriptManagementAPI.ScriptManagementAPIBase.ReadScript(ReadScriptRequest, ServerCallContext)"/>
    public override Task<ReadScriptResponse> ReadScript(ReadScriptRequest request, ServerCallContext context)
    {
        if (request.Name.IsNullOrEmpty())
            return Task.FromResult(
                new ReadScriptResponse
                {
                    ScriptName = request.Name,
                    ScriptType = request.Type,
                    Success = false,
                    ErrorMessage = "The parameter [name] is required!"
                }
            );

        try
        {
            var scriptPath = GridServerFileHelper.GetGridServerScriptPath(
                request.Name,
                (Grid.ScriptType)request.Type
            );

            if (!File.Exists(scriptPath))
                return Task.FromResult(
                    new ReadScriptResponse
                    {
                        ScriptName = request.Name,
                        ScriptType = request.Type,
                        ScriptPath = scriptPath,
                        Success = false,
                        ErrorMessage = $"The script [{request.Name}] does not exist at path [{scriptPath}]!"
                    }
                );

            return Task.FromResult(
                new ReadScriptResponse
                {
                    ScriptName = request.Name,
                    ScriptType = request.Type,
                    Success = true,
                    ScriptPath = scriptPath,
                    ScriptContents = File.ReadAllText(scriptPath)
                }
            );
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                new ReadScriptResponse
                {
                    ScriptName = request.Name,
                    ScriptType = request.Type,
                    Success = false,
                    ErrorMessage = ex.Message
                }
            );
        }
    }

    /// <inheritdoc cref="ScriptManagementAPI.ScriptManagementAPIBase.DeleteScript(DeleteScriptRequest, ServerCallContext)"/>
    public override Task<DeleteScriptResponse> DeleteScript(DeleteScriptRequest request, ServerCallContext context)
    {
        if (request.Name.IsNullOrEmpty())
            return Task.FromResult(
                new DeleteScriptResponse
                {
                    ScriptName = request.Name,
                    ScriptType = request.Type,
                    Success = false,
                    ErrorMessage = "The parameter [name] is required!"
                }
            );

        try
        {
            var scriptPath = GridServerFileHelper.GetGridServerScriptPath(
                request.Name,
                (Grid.ScriptType)request.Type
            );

            if (!File.Exists(scriptPath))
                return Task.FromResult(
                    new DeleteScriptResponse
                    {
                        ScriptName = request.Name,
                        ScriptType = request.Type,
                        ScriptPath = scriptPath,
                        Success = false,
                        ErrorMessage = $"The script [{request.Name}] does not exist at path [{scriptPath}]!"
                    }
                );

            scriptPath.PollDeletionBlocking();

            return Task.FromResult(
                new DeleteScriptResponse
                {
                    ScriptName = request.Name,
                    ScriptType = request.Type,
                    Success = true,
                    ScriptPath = scriptPath
                }
            );
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                new DeleteScriptResponse
                {
                    ScriptName = request.Name,
                    ScriptType = request.Type,
                    Success = false,
                    ErrorMessage = ex.Message
                }
            );
        }
    }
}
