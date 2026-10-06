namespace Grid.Bot;

using System;

/// <summary>
/// Options for the render and script execution rate limiters.
/// </summary>
public class FloodCheckerOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "FloodChecker";

    /// <summary>
    /// The vault path for the flood checker options.
    /// </summary>
    public const string VaultPath = "floodcheckers";

    /// <summary>
    /// Gets or sets the limit for the script execution flood checker.
    /// </summary>
    public int ScriptExecutionFloodCheckerLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the window for the script execution flood checker.
    /// </summary>
    public TimeSpan ScriptExecutionFloodCheckerWindow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets a value indicating whether the script execution flood checker is enabled.
    /// </summary>
    public bool ScriptExecutionFloodCheckingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the limit for the render flood checker.
    /// </summary>
    public int RenderFloodCheckerLimit { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the window for the render flood checker.
    /// </summary>
    public TimeSpan RenderFloodCheckerWindow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets a value indicating whether the render flood checker is enabled.
    /// </summary>
    public bool RenderFloodCheckingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the limit for the per user script execution flood checker.
    /// </summary>
    public int ScriptExecutionPerUserFloodCheckerLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the window for the per user script execution flood checker.
    /// </summary>
    public TimeSpan ScriptExecutionPerUserFloodCheckerWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets a value indicating whether the per user script execution flood checker is enabled.
    /// </summary>
    public bool ScriptExecutionPerUserFloodCheckingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the limit for the per user render flood checker.
    /// </summary>
    public int RenderPerUserFloodCheckerLimit { get; set; } = 50;

    /// <summary>
    /// Gets or sets the window for the per user render flood checker.
    /// </summary>
    public TimeSpan RenderPerUserFloodCheckerWindow { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets a value indicating whether the per user render flood checker is enabled.
    /// </summary>
    public bool RenderPerUserFloodCheckingEnabled { get; set; }
}
