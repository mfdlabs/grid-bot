namespace Grid;

/// <inheritdoc cref="IJob"/>
public class Job : IJob
{
    /// <inheritdoc cref="IJob.Id"/>
    public string Id { get; }

    /// <summary>
    /// Construct a new instance of <see cref="Job"/>
    /// </summary>
    /// <param name="id">The ID of the job.</param>
    public Job(string id)
    {
        Id = id;
    }

    /// <inheritdoc cref="System.Object.Equals(object)"/>
    public override bool Equals(object obj) => obj is Job job && job.Id == Id;

    /// <inheritdoc cref="System.Object.GetHashCode"/>
    public override int GetHashCode() => Id.GetHashCode();

    /// <inheritdoc cref="System.Object.ToString"/>
    public override string ToString() => $"Job ID = {this.Id}";
}
