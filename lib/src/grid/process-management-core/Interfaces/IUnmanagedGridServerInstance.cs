namespace Grid;

/// <summary>
/// Represents an unmanaged Grid Server Instance.
/// </summary>
public interface IUnmanagedGridServerInstance
{
    /// <summary>
    /// The ID of the instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Kill the RCC instance.
    /// </summary>
    void Kill();
}
