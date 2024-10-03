namespace Grid.Bot.Commands.Public;

using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;

using Discord.Commands;

using Text.Extensions;

using Utility;
using Extensions;




/// <summary>
/// Interaction handler for the support commands.
/// </summary>
public class Help : ModuleBase
{
    private readonly IAdminUtility _adminUtility;
    private readonly CommandsSettings _commandsSettings;

    private readonly HashSet<(string[] aliases, Embed embed, BotRole role)> _aliasesToEmbeds = new();

    /// <summary>
    /// Construct a new instance of <see cref="Support"/>.
    /// </summary>
    /// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
    /// <param name="commandService">The <see cref="CommandService"/>.</param>
    /// <param name="commandsSettings">The <see cref="CommandsSettings"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="adminUtility"/> cannot be null.
    /// - <paramref name="commandService"/> cannot be null.
    /// - <paramref name="commandsSettings"/> cannot be null.
    /// </exception>
    public Help(
        IAdminUtility adminUtility,
        CommandService commandService,
        CommandsSettings commandsSettings
    )
    {
        ArgumentNullException.ThrowIfNull(commandService, nameof(commandService));
        _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
        _commandsSettings = commandsSettings ?? throw new ArgumentNullException(nameof(commandsSettings));

        SetupCache(commandService, commandsSettings);
    }


    private void SetupCache(CommandService commandService, CommandsSettings commandsSettings)
    {
        foreach (var module in commandService.Modules)
        {
            bool isGrouped = !string.IsNullOrEmpty(module.Group);
            var aliases = new HashSet<string>();

            if (isGrouped)
            {
                aliases.Add(module.Group);
                aliases.UnionWith(module.Aliases);
            }
            else
            {
                aliases.Add(module.Commands[0].Name);
                aliases.UnionWith(module.Commands[0].Aliases);
            }

            var botRoleAttribute = module.Attributes.FirstOrDefault(attrib => attrib.GetType() == typeof(RequireBotRoleAttribute)) as RequireBotRoleAttribute;
            var requiredPermission = botRoleAttribute?.BotRole ?? BotRole.Default;

            var builder = new EmbedBuilder().WithColor(Color.Green).WithTimestamp(DateTimeOffset.Now);

            if (isGrouped)
            {
                var title = module.Group;

                if (module.Aliases.Skip(1).Count() > 1)
                    title += string.Format(" - {0}", module.Aliases.Skip(1).Join(", "));

                builder.WithTitle(title);

                if (!string.IsNullOrEmpty(module.Summary))
                    builder.WithDescription(module.Summary);

                foreach (var command in module.Commands)
                    builder.AddField(field =>
                    {
                        var fieldName = command.Name;

                        var commandAliases = command.Aliases.Select(alias => alias.Split(' ').ElementAt(1)).Skip(1).Distinct();
                        if (commandAliases.Count() > 1)
                            fieldName += string.Format(" - {0}", commandAliases.Join(", "));

                        field.WithName(fieldName);

                        var fieldValue = "";

                        if (command.Parameters.Count > 0)
                            fieldValue += string.Format(
                                "Command Arguments:\n{0}{1} {2} ",
                                commandsSettings.Prefix,
                                module.Group,
                                command.Name
                            );

                        foreach (var parameter in command.Parameters)
                        {
                            if (parameter.IsOptional)
                                fieldValue += string.Format("<*{0}*>", parameter.Name);
                            else
                                fieldValue += string.Format("<***{0}***>", parameter.Name);

                            if (parameter.IsOptional)
                                if (parameter.DefaultValue is null || (parameter.DefaultValue is string str && string.IsNullOrEmpty(str)))
                                    fieldValue += "?";
                                else
                                    fieldValue += string.Format("[=*{0}*]", parameter.DefaultValue);

                            if (parameter.IsRemainder)
                                fieldValue += "... ";
                            else
                                fieldValue += " ";
                        }

                        if (!string.IsNullOrEmpty(command.Summary))
                            fieldValue += string.Format("\n\n{0}", command.Summary);

                        field.WithValue(fieldValue);
                    });
            }
            else
            {
                var command = module.Commands[0];

                var title = command.Name;

                if (command.Aliases.Skip(1).Count() > 1)
                    title += string.Format(" - {0}", command.Aliases.Skip(1).Join(", "));

                builder.WithTitle(title);

                var description = "";

                if (command.Parameters.Count > 0)
                    description += string.Format(
                        "Command Arguments:\n{0}{1} ",
                        commandsSettings.Prefix,
                        command.Name
                    );

                foreach (var parameter in command.Parameters)
                {
                    if (parameter.IsOptional)
                        description += string.Format("<*{0}*>", parameter.Name);
                    else
                        description += string.Format("<***{0}***>", parameter.Name);

                    if (parameter.IsOptional)
                        if (parameter.DefaultValue is null || (parameter.DefaultValue is string str && string.IsNullOrEmpty(str)))
                            description += "?";
                        else
                            description += string.Format("[=*{0}*]", parameter.DefaultValue);

                    if (parameter.IsRemainder)
                        description += "... ";
                    else
                        description += " ";
                }

                if (!string.IsNullOrEmpty(command.Summary))
                    description += string.Format("\n\n{0}", command.Summary);

                builder.WithDescription(description);
            }

            _aliasesToEmbeds.Add((aliases.ToArray(), builder.Build(), requiredPermission));
        }
    }

    /// <summary>
    /// Gets brief help for all commands or a specific command.
    /// </summary>
    /// <param name="commandName">The optional command name</param>
    [Command("help"), Summary("Gets help for commands.")]
    public async Task GetHelp(
        [Summary("The optional command name.")]
        string commandName = ""
    )
    {
        if (!string.IsNullOrEmpty(commandName))
        {
            // Grouped modules (such as client settings) will have their aliases and group set.
            // If they aren't then we assume it has only one command.

            var (embed, role) = _aliasesToEmbeds
                                    .Where(tup => tup.aliases.Contains(commandName.ToLowerInvariant()))
                                    .Select(tup => (tup.embed, tup.role))
                                    .FirstOrDefault();

            if (embed == null)
            {
                await this.ReplyWithReferenceAsync($"Unable to find command with name '{commandName}'!");

                return;
            }

            if (!_adminUtility.IsInRole(Context.User, role)) return;

            await this.ReplyWithReferenceAsync(embed: embed);

            return;
        }

        var currentEmbed = new EmbedBuilder()
            .WithTitle("Commands")
            .WithDescription($"Use the {_commandsSettings.Prefix}help <commandName> to see more information on commands marked as **grouped**.")
            .WithColor(Color.Green)
            .WithTimestamp(DateTimeOffset.Now);

        var embeds = new List<EmbedBuilder>();

        for (int i = 1; i <= _aliasesToEmbeds.Count; i++)
        {
            var (aliases, embed, role) = _aliasesToEmbeds.ElementAt(i - 1);
            if (!_adminUtility.IsInRole(Context.User, role)) continue;

            var fieldName = aliases.First();

            if (embed.Fields.Length > 0) fieldName += " - **grouped**";

            currentEmbed.AddField(fieldName, embed.Description);

            // For every 24 fields, add to the list and reset current embed.
            if (i % EmbedBuilder.MaxFieldCount == 0)
            {
                embeds.Add(currentEmbed);
                currentEmbed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTimestamp(DateTimeOffset.Now);
            }
        }

        if (embeds.Count == 0) embeds.Add(currentEmbed);

        embeds.Last().WithAuthor(Context.User);

        await this.ReplyWithReferenceAsync("Help for the commands:", embed: embeds.First().Build());

        embeds = embeds.Skip(1).ToList();
        foreach (var embed in embeds) await this.ReplyWithReferenceAsync(embed: embed.Build());
    }
}
