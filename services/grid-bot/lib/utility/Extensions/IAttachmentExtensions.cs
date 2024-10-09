namespace Grid.Bot.Extensions;

using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Discord;

/// <summary>
/// Extension methods for <see cref="IAttachment"/>
/// </summary>
public static class IAttachmentExtensions
{
    /// <summary>
    /// Gets the data for the <see cref="IAttachment"/>
    /// </summary>
    /// <param name="attachment">The <see cref="IAttachment"/></param>
    /// <returns>The raw bytes of the <see cref="IAttachment"/></returns>
    public static async Task<byte[]> GetRawAttachmentBuffer(this IAttachment attachment)
    {
        using var client = new HttpClient();

        return await client.GetByteArrayAsync(attachment.Url);
    }

    /// <summary>
    /// Gets the data from the <see cref="IAttachment"/> and returns an ASCII string.
    /// </summary>
    /// <param name="attachment">The <see cref="IAttachment"/></param>
    /// <returns>The raw string.</returns>
    public static async Task<string> GetAttachmentContentsAscii(this IAttachment attachment) 
        => Encoding.ASCII.GetString(await attachment.GetRawAttachmentBuffer());
}
