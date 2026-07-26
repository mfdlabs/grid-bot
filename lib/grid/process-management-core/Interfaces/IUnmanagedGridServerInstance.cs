namespace Grid.ProcessManagement.Core;

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
    /// Kill the Grid Server instance.
    /// </summary>
    void Kill();
}
