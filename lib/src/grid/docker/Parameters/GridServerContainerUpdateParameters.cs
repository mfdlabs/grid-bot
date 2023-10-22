namespace Grid;

using Docker.DotNet.Models;


/// <inheritdoc cref="ContainerUpdateParameters"/>
public class GridServerContainerUpdateParameters : ContainerUpdateParameters
{
    /// <summary>
    /// The ID of the container.
    /// </summary>
    public string ContainerId { get; set; }

    /// <inheritdoc cref="System.Object.ToString"/>
    public override string ToString() => $"ContainerId = {ContainerId}, CPUQuota = {CPUQuota}, Memory = {Memory}";
}
