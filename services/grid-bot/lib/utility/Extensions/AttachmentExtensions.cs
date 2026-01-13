namespace Grid.Bot.Extensions;

using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using Discord;

/// <summary>
/// Extension methods for <see cref="IAttachment"/>
/// </summary>
public static class AttachmentExtensions
{
    /// <param name="attachment">The <see cref="IAttachment"/></param>
    extension(IAttachment attachment)
    {
        /// <summary>
        /// Gets the data for the <see cref="IAttachment"/>
        /// </summary>
        /// <returns>The raw bytes of the <see cref="IAttachment"/></returns>
        private async Task<byte[]> GetRawAttachmentBuffer()
        {
            using var client = new HttpClient();

            return await client.GetByteArrayAsync(attachment.Url);
        }

        /// <summary>
        /// Gets the data from the <see cref="IAttachment"/> and returns an ASCII string.
        /// </summary>
        /// <returns>The raw string.</returns>
        public async Task<string> GetAttachmentContentsAscii() 
            => Encoding.ASCII.GetString(await attachment.GetRawAttachmentBuffer());
    }
}
