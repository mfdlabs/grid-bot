namespace Grid;

using System;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32;

/// <summary>
/// A helper class for file-system based operations on a grid server.
/// </summary>
public static class GridServerFileHelper
{
    /// <summary>
    /// Get the grid server's base path.
    /// </summary>
    /// <param name="throwIfNoGridServer">Should an exception be thrown if the grid server is not installed?</param>
    /// <returns>The grid server's base path.</returns>
    /// <exception cref="ApplicationException">The grid server was not correctly installed on the machine.</exception>
    public static string GetGridServerPath(bool throwIfNoGridServer = true)
    {
        object value;

        // Check if the current runtime is on a Windows machine
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            value = Registry.GetValue(
                global::Grid.Properties.Settings.Default.GridServerRegistryKeyName,
                global::Grid.Properties.Settings.Default.GridServerRegistryValueName,
                null
            );
        }
        else
        {
            value = Environment.GetEnvironmentVariable("GRID_SERVER_PATH");
        }

        if (value != null) return value as string;
        if (throwIfNoGridServer)
            throw new ApplicationException(global::Grid.Properties.Resources.GridServerNotCorrectlyInstalled);
        
        return default;
    }

    /// <summary>
    /// Get the grid server's full path to the executable.
    /// </summary>
    /// <param name="throwIfNoGridServer">Should an exception be thrown if the grid server is not installed?</param>
    /// <returns>The grid server's full path to the executable.</returns>
    public static string GetFullyQualifiedGridServerPath(bool throwIfNoGridServer = true)
        => Path.Combine(GetGridServerPath(throwIfNoGridServer), global::Grid.Properties.Settings.Default.GridServerExecutableName);

    /// <summary>
    /// Get the fully qualified path to a grid server Lua script.
    /// </summary>
    /// <param name="scriptName">The name of the script.</param>
    /// <param name="scriptType">The type of the script. Defaults to <see cref="ScriptType.InternalScript"/>.</param>
    /// <param name="throwIfNoGridServer">Should an exception be thrown if the grid server is not installed?</param>
    /// <param name="test">Should the path constructed be tested for existence?</param>
    /// <returns>The fully qualified path to the script.</returns>
    /// <exception cref="ApplicationException">Unable to find the grid server's base path.</exception>
    public static string GetGridServerScriptPath(
        string scriptName,
        ScriptType scriptType = ScriptType.InternalScript,
        bool throwIfNoGridServer = true,
        bool test = false
    )
    {
        var prefix = GetGridServerPrefixByScriptType(scriptType, throwIfNoGridServer);

        scriptName = scriptName.Replace("..", "");
        var fullPath = Path.Combine(prefix, $"{scriptName}.lua");

        if (!test) return fullPath;

        if (!File.Exists(fullPath))
            throw new ApplicationException(string.Format(global::Grid.Properties.Resources.CouldNotFindGridServerLuaScript, scriptName, prefix));

        return fullPath;
    }

    /// <summary>
    /// Get the fully qualified name of the directory by it's <see cref="ScriptType"/>.
    /// </summary>
    /// <param name="scriptType">The type of the script.</param>
    /// <param name="throwIfNoGridServer">Should an exception be thrown if the grid server is not installed?</param>
    /// <returns>The fully qualified name of the directory by it's <see cref="ScriptType"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="ScriptType"/> is not supported.</exception>
    public static string GetGridServerPrefixByScriptType(ScriptType scriptType, bool throwIfNoGridServer = true)
    {
        var prefix = GetGridServerPath(throwIfNoGridServer);
        var scriptPart = scriptType switch
        {
            ScriptType.InternalScript => Path.Combine("internalscripts", "scripts"),
            ScriptType.InternalModule => Path.Combine("internalscripts", "modules"),
            ScriptType.ThumbnailScript => Path.Combine("internalscripts", "thumbnails"),
            ScriptType.ThumbnailModule => Path.Combine("internalscripts", "thumbnails", "modules"),
            ScriptType.LuaPackage => Path.Combine("ExtraContent", "LuaPackges"),
            ScriptType.SharedCoreScript => Path.Combine("ExtraContent", "scripts", "CoreScripts"),
            ScriptType.ClientCoreScript => Path.Combine("ExtraContent", "scripts", "CoreScripts", "CoreScripts"),
            ScriptType.CoreModule => Path.Combine("ExtraContent", "scripts", "CoreScripts", "Modules"),
            ScriptType.ServerCoreScript => Path.Combine("ExtraContent", "scripts", "CoreScripts", "ServerCoreScripts"),
            ScriptType.StarterCharacterScript => Path.Combine("ExtraContent", "scripts", "PlayerScripts", "StarterCharacterScripts"),
            ScriptType.StarterPlayerScript => Path.Combine("ExtraContent", "scripts", "PlayerScripts", "StarterPlayerScripts"),
            ScriptType.StarterPlayerScriptNewStructure => Path.Combine("ExtraContent", "scripts", "PlayerScripts", "StarterPlayerScripts_NewStructure"),
            ScriptType.StarterPlayerScriptCommon => Path.Combine("ExtraContent", "scripts", "PlayerScripts", "StarterPlayerScriptsCommon"),
            ScriptType.HiddenCommon => Path.Combine("ExtraContent", "hidden", "common"),
            ScriptType.Hidden => Path.Combine("Content", "hidden", "rcc"),
            ScriptType.HiddenModule => Path.Combine("Content", "hidden", "rcc", "modules"),
            _ => throw new ArgumentOutOfRangeException(nameof(scriptType), scriptType, null)
        };

        return Path.Combine(prefix, scriptPart);
    }
}
