import { Message } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { CommandRegistry } from '../CommandUtility/CommandRegistry';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class Help implements StateSpecificCommandHandler {
	public CommandName = 'Help';
	public CommandDescription = 'Print the help embed for all commands or a specific command.';
	public Command = ['h', 'help'];
	public Callback = async (messageContent: string[], messageRoot: Message) => {
		const commandName = messageContent[0];

		if (commandName !== undefined) {
			const embed = await CommandRegistry.ConstructHelpEmbedForSingleCommand(commandName, messageRoot.author.id);

			if (embed === null) {
				if (GlobalConfig.IsAllowedToEchoBackNotFoundCommandException) {
					return messageRoot.reply(`The command with the name '${commandName}' was not found.`);
				}
			}

			return messageRoot.channel.send(embed);
		}

		return messageRoot.channel.send(await CommandRegistry.ConstructHelpEmbedForCommands(messageRoot.author.id));
	};
}

export = new Help();
