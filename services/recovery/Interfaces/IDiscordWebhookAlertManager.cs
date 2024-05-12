namespace Grid.Bot;

using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;

/// <summary>
/// Handles sending alerts to a Discord webhook.
/// </summary>
public interface IDiscordWebhookAlertManager
{
    /// <summary>
    /// Sends an alert to the Discord webhook.
    /// </summary>
    /// <param name="topic">The topic of the alert.</param>
    /// <param name="message">The message of the alert.</param>
    /// <param name="color">The color of the alert.</param>
    /// <param name="attachments">The attachments of the alert.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SendAlertAsync(string topic, string message, Color? color = null, IEnumerable<FileAttachment> attachments = null);
}
