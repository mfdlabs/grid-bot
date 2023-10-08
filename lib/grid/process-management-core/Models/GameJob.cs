namespace Grid;

/// <summary>
/// Implementation for an Grid Server game job.
/// </summary>
public class GameJob : Job
{
    /// <summary>
    /// The ID of the place.
    /// </summary>
    public long PlaceId { get; }

    /// <summary>
    /// Construct a new instance of <see cref="GameJob"/>
    /// </summary>
    /// <param name="id">The ID of the job..</param>
    /// <param name="placeId">The ID of the place.</param>
    public GameJob(string id, long placeId)
        : base(id)
    {
        PlaceId = placeId;
    }

    /// <inheritdoc cref="System.Object.ToString"/>
    public override string ToString() => $"{base.ToString()}, PlaceID = {PlaceId}";
}
