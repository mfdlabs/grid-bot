namespace Grid;

using System;

/// <summary>
/// A helper class for file-system based operations on a grid server.
/// </summary>
public interface IGridServerFileHelper
{
    /// <summary>
    /// Get the grid server's full path to the executable.
    /// </summary>
    /// <param name="throwIfNoGridServer">Should an exception be thrown if the grid server is not installed?</param>
    /// <returns>The grid server's full path to the executable.</returns>
    string GetFullyQualifiedGridServerPath(bool throwIfNoGridServer = true);

    /// <summary>
    /// Get the grid server's base path.
    /// </summary>
    /// <param name="throwIfNoGridServer">Should an exception be thrown if the grid server is not installed?</param>
    /// <returns>The grid server's base path.</returns>
    /// <exception cref="ApplicationException">The grid server was not correctly installed on the machine.</exception>
    string GetGridServerPath(bool throwIfNoGridServer = true);

    /// <summary>
    /// Get the fully qualified name of the directory by it's <see cref="ScriptType"/>.
    /// </summary>
    /// <param name="scriptType">The type of the script.</param>
    /// <param name="throwIfNoGridServer">Should an exception be thrown if the grid server is not installed?</param>
    /// <returns>The fully qualified name of the directory by it's <see cref="ScriptType"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="ScriptType"/> is not supported.</exception>
    string GetGridServerPrefixByScriptType(ScriptType scriptType, bool throwIfNoGridServer = true);

    /// <summary>
    /// Get the fully qualified path to a grid server Lua script.
    /// </summary>
    /// <param name="scriptName">The name of the script.</param>
    /// <param name="scriptType">The type of the script. Defaults to <see cref="ScriptType.InternalScript"/>.</param>
    /// <param name="throwIfNoGridServer">Should an exception be thrown if the grid server is not installed?</param>
    /// <param name="test">Should the path constructed be tested for existence?</param>
    /// <returns>The fully qualified path to the script.</returns>
    /// <exception cref="ApplicationException">Unable to find the grid server's base path.</exception>
    string GetGridServerScriptPath(string scriptName, ScriptType scriptType = ScriptType.InternalScript, bool throwIfNoGridServer = true, bool test = false);
}
