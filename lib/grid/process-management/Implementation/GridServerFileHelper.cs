namespace Grid;

using System;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32;

/// <summary>
/// A helper class for file-system based operations on a grid server.
/// </summary>
public class GridServerFileHelper : IGridServerFileHelper
{
    private readonly IGridServerProcessSettings _Settings;

    /// <summary>
    /// Construct a new instance of <see cref="GridServerFileHelper"/>
    /// </summary>
    /// <param name="settings">The <see cref="IGridServerProcessSettings"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> cannot be null.</exception>
    public GridServerFileHelper(IGridServerProcessSettings settings)
    {
        _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc cref="IGridServerFileHelper.GetGridServerPath(bool)"/>
    public string GetGridServerPath(bool throwIfNoGridServer = true)
    {
        object value;

        // Check if the current runtime is on a Windows machine
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            value = Registry.GetValue(
                _Settings.GridServerRegistryKeyName,
                _Settings.GridServerRegistryValueName,
                null
            );
        }
        else
        {
            value = Environment.GetEnvironmentVariable("GRID_SERVER_PATH");
        }

        if (value != null) return value as string;
        if (throwIfNoGridServer)
            throw new ApplicationException("The grid server was not correctly installed on the machine.");

        return default;
    }

    /// <inheritdoc cref="IGridServerFileHelper.GetFullyQualifiedGridServerPath(bool)"/>
    public string GetFullyQualifiedGridServerPath(bool throwIfNoGridServer = true)
        => Path.Combine(GetGridServerPath(throwIfNoGridServer), _Settings.GridServerExecutableName);

    /// <inheritdoc cref="IGridServerFileHelper.GetGridServerScriptPath(string, ScriptType, bool, bool)"/>
    public string GetGridServerScriptPath(
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
            throw new ApplicationException(string.Format("Unable to find the script file '{0}' at the path '{1}'.", scriptName, prefix));

        return fullPath;
    }

    /// <inheritdoc cref="IGridServerFileHelper.GetGridServerPrefixByScriptType(ScriptType, bool)"/>
    public string GetGridServerPrefixByScriptType(ScriptType scriptType, bool throwIfNoGridServer = true)
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
