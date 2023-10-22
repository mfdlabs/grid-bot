namespace Grid.Bot.Utility;

using FloodCheckers.Core;

/// <summary>
///    Interface for a flood checker registry.
/// </summary>
public interface IFloodCheckerRegistry
{
    /// <summary>
    /// Gets the system flood checker for script executions.
    /// </summary>
    IFloodChecker ScriptExecutionFloodChecker { get; }

    /// <summary>
    /// Gets the system flood checker for renders.
    /// </summary>
    IFloodChecker RenderFloodChecker { get; }

    /// <summary>
    /// Get the script execution <see cref="IFloodChecker"/> for the <see cref="Discord.IUser"/>
    /// </summary>
    /// <param name="userId">The ID of the <see cref="Discord.IUser"/></param>
    /// <returns>The script execution <see cref="IFloodChecker"/></returns>
    IFloodChecker GetPerUserScriptExecutionFloodChecker(ulong userId);

    /// <summary>
    /// Get the render <see cref="IFloodChecker"/> for the <see cref="Discord.IUser"/>
    /// </summary>
    /// <param name="userId">The ID of the <see cref="Discord.IUser"/></param>
    /// <returns>The render <see cref="IFloodChecker"/></returns>
    IFloodChecker GetPerUserRenderFloodChecker(ulong userId);
}
