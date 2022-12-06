import { Message } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { AdminUtil } from '../Administration/AdminUtil';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { Logger } from '../Logging/LoggingUtility';

class Restart implements StateSpecificCommandHandler {
	public CommandName = 'Restart Bot';
	public CommandDescription = 'Restart Bot';
	public Internal = true;
	public Command = ['re', 'restart'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;
		messageRoot
			.reply('Restarting...')
			.then(() => {
				process.emit('SIGUSR2', 'SIGUSR2');
				GlobalConfig.Bot.destroy();
			})
			.then(async () => {
				GlobalConfig.Bot.login(GlobalConfig.BotToken);
				Logger.Debug("Logged in as '%s'", GlobalConfig.Bot.user.tag);
			});
	};
}

export = new Restart();
