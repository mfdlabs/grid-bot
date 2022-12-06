import { DMChannel, NewsChannel, TextChannel } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';

export class Help {
	public static HandlePrefixCheck(channel: TextChannel | DMChannel | NewsChannel) {
		channel.send(`The Current prefix is: \`${GlobalConfig.Prefix}\``);
	}
}
