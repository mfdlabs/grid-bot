namespace Grid.Bot;

using System;

using Microsoft.Extensions.Configuration;

using VaultSharp;

/// <summary>
/// Extensions for adding Vault as a configuration source.
/// </summary>
public static class VaultConfigurationExtensions
{
    /// <summary>
    /// Adds Vault KV v2 secrets as a configuration source.
    /// </summary>
    /// <remarks>
    /// Later sources win, so add this before environment variables to let them override it.
    /// <code>
    /// builder.AddVault(client, "grid-bot-settings", s =>
    /// {
    ///     s.PathPrefix = "prod";
    ///     s.Map("Global", "global");   // grid-bot-settings/prod/global -> Global:*
    ///     s.Map("Discord", "discord");
    /// });
    /// </code>
    /// </remarks>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <param name="client">The Vault client.</param>
    /// <param name="mount">The KV v2 mount point.</param>
    /// <param name="configure">Configures the secrets to read.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="builder"/> cannot be null.
    /// - <paramref name="client"/> cannot be null.
    /// - <paramref name="configure"/> cannot be null.
    /// </exception>
    public static IConfigurationBuilder AddVault(
        this IConfigurationBuilder builder,
        IVaultClient client,
        string mount,
        Action<VaultConfigurationSource> configure)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var source = new VaultConfigurationSource { Client = client, Mount = mount };

        configure(source);

        return builder.Add(source);
    }
}
