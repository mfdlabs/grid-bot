namespace Grid.Bot;

using System.Threading.Tasks;

/// <summary>
/// Manager for the bot.
/// </summary>
public interface IBotManager
{
    /// <summary>
    /// Start the bot.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stop the bot.
    /// </summary>
    Task StopAsync();
}
