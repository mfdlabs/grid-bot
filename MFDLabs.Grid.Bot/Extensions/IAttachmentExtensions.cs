using System.Net;
using System.Text;
using Discord;

namespace MFDLabs.Grid.Bot.Extensions
{
    internal static class AttachmentExtensions
    {
        public static byte[] GetRawAttachmentBuffer(this IAttachment attachment)
        {
            using var client = new WebClient();
            return client.DownloadData(attachment.Url);
        }

        public static string GetAttachmentContentsUtf8(this IAttachment attachment) 
            => Encoding.UTF8.GetString(attachment.GetRawAttachmentBuffer());
        public static string GetAttachmentContentsAscii(this IAttachment attachment) 
            => Encoding.ASCII.GetString(attachment.GetRawAttachmentBuffer());
    }
}
