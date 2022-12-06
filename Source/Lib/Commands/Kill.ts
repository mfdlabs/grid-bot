import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { Logger } from '../Logging/LoggingUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class CloseBot implements StateSpecificCommandHandler {
	public CommandName = 'Kill';
	public CommandDescription = 'Kills the bot';
	public Internal = true;
	public Command = ['k', 'kill'];
	public Callback = async (messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		messageRoot.reply('Killing process.');
		Logger.Warn('Emitting SIGTERM request to process with code 0');
		process.emit('SIGTERM', 'SIGTERM');
	};
}

export = new CloseBot();
