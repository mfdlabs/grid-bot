namespace MFDLabs.Grid;

/// <summary>
/// Represents the type of script.
/// </summary>
public enum ScriptType
{
    /// <summary>
    /// //internalscripts/scripts
    /// </summary>
    InternalScript,

    /// <summary>
    /// //internalscripts/modules
    /// </summary>
    InternalModule,

    /// <summary>
    /// //internalscripts/thumbnails
    /// </summary>
    ThumbnailScript,

    /// <summary>
    /// //internalscripts/thumbnails/modules
    /// </summary>
    ThumbnailModule,

    /// <summary>
    /// //ExtraContent/LuaPackages
    /// </summary>
    LuaPackage,

    /// <summary>
    /// //ExtraContent/scripts/CoreScripts
    /// </summary>
    SharedCoreScript,

    /// <summary>
    /// //ExtraContent/scripts/CoreScripts/CoreScripts
    /// </summary>
    ClientCoreScript,

    /// <summary>
    /// //ExtraContent/scripts/CoreScripts/Modules
    /// </summary>
    CoreModule,

    /// <summary>
    /// //ExtraContent/scripts/CoreScripts/ServerCoreScripts
    /// </summary>
    ServerCoreScript,

    /// <summary>
    /// //ExtraContent/scripts/PlayerScripts/StarterCharacterScripts
    /// </summary>
    StarterCharacterScript,

    /// <summary>
    /// //ExtraContent/scripts/PlayerScripts/StarterPlayerScripts
    /// </summary>
    StarterPlayerScript,

    /// <summary>
    /// //ExtraContent/scripts/PlayerScripts/StarterPlayerScripts_NewStructure
    /// </summary>
    StarterPlayerScriptNewStructure,

    /// <summary>
    /// //ExtraContent/scripts/PlayerScripts/StarterPlayerScriptsCommon
    /// </summary>
    StarterPlayerScriptCommon,

    /// <summary>
    /// //ExtraContent/hidden/common
    /// </summary>
    HiddenCommon,

    /// <summary>
    /// //Content/hidden/rcc
    /// </summary>
    Hidden,

    /// <summary>
    /// //Content/hidden/rcc/modules
    /// </summary>
    HiddenModule
}
