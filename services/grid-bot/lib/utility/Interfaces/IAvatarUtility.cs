namespace Grid.Bot.Utility;

using System.IO;

/// <summary>
/// Utility class for rendering avatars.
/// </summary>
public interface IAvatarUtility
{
    /// <summary>
    /// Render a user by ID.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="placeId">The place ID to inherit character settings from.</param>
    /// <param name="sizeX">The X dimension of the image.</param>
    /// <param name="sizeY">The Y dimension of the image.</param>
    /// <returns>The stream and thumbnail name.</returns>
    (Stream, string) RenderUser(long userId, long placeId, int sizeX, int sizeY);
}
