namespace MFDLabs.Grid.Commands;

/// <summary>
/// The command to run a micro profiler on the grid
/// </summary>
public class RunMicroProfilerCommand : GridCommand
{
    /// <inheritdoc cref="GridCommand.Mode"/>
    public override string Mode => "RunMicroProfiler";

    /// <inheritdoc cref="GridCommand.MessageVersion"/>
    public override int MessageVersion => 1;

    /// <summary>
    /// The settings for the command
    /// </summary>
    public RunMicroProfilerSettings Settings { get; }

    /// <summary>
    /// Construct a new instance of <see cref="RunMicroProfilerCommand"/>
    /// </summary>
    /// <param name="settings">The settings for the command</param>
    public RunMicroProfilerCommand(RunMicroProfilerSettings settings) => Settings = settings;
}
