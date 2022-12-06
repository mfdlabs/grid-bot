import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class CloseAllJobs implements StateSpecificCommandHandler {
	public CommandName = 'Close All Jobs';
	public CommandDescription = 'Closes all jobs';
	public Internal = true;
	public Command = ['caj', 'closealljobs', 'closeallgames'];
	public Callback = async (messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		await SOAPUtil.CloseAllJobs();
		return messageRoot.reply(`Successfully closed all jobs.`);
	};
}

export = new CloseAllJobs();
