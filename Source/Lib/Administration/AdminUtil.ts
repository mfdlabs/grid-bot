import { Message } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { Logger } from '../Logging/LoggingUtility';

export class AdminUtil {
	public static CheckIsUserInAdminWhiteList(id: string) {
		return GlobalConfig.WhitelistedUsers.includes(id);
	}

	public static CheckIsChannelInChannelWhiteList(id: string) {
		return GlobalConfig.WhiteListedChannelIds.includes(id);
	}

	public static RejectIfNotAdmin(message: Message) {
		if (!AdminUtil.CheckIsUserInAdminWhiteList(message.author.id)) {
			Logger.Warn("User '%s' is not on the admin whitelist.", message.author.id);
			message.reply('You lack the correct permissions to execute that command.');
			return false;
		}
		Logger.Info("User '%s' is on the admin whitelist.", message.author.id);
		return true;
	}
}
