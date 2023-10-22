namespace Grid;

/// <summary>
/// Represents a Grid Server Job.
/// </summary>
public interface IJob
{
    /// <summary>
    /// The ID of the job.
    /// </summary>
    string Id { get; }
}
