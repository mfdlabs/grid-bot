import { Message, MessageMentions } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { AdminUtil } from '../Administration/AdminUtil';
import { CommandRegistry } from '../CommandUtility/CommandRegistry';

export class Messages {
	public static async MessageSent(message: Message): Promise<void> {
		if (message.author.bot && !GlobalConfig.AllowParsingForBots) return;

		if (!GlobalConfig.AllowAllChannels) {
			if (!AdminUtil.CheckIsChannelInChannelWhiteList(message.channel.id)) {
				return;
			}
		}

		let content: string;

		if ((content = Messages.ParsePrefix(message.content, message.mentions)) === null) return;

		if (!GlobalConfig.IsEnabled) {
			if (!AdminUtil.CheckIsUserInAdminWhiteList(message.author.id)) {
				if (GlobalConfig.ReasonForDying) message.reply(GlobalConfig.ReasonForDying);
				return;
			}
		}

		let contentArray = Messages.ParseMessage(content);

		await Messages.HandleCommand(contentArray, message);
	}

	private static async HandleCommand(messageContent: string[], message: Message) {
		const command = messageContent[0].toLowerCase();

		return await CommandRegistry.CheckAndRunCommand(command, messageContent.slice(1), message);
	}

	private static ParsePrefix(content: string, mentions: MessageMentions): string {
		let message = content;
		if (!content.startsWith(GlobalConfig.Prefix)) {
			return null;
		} else {
			message = message.substring(1);
		}
		return message;
	}

	private static ParseMessage(content: string): string[] {
		return content.includes(' ') ? content.split(' ') : [content];
	}
}
