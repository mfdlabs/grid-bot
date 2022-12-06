import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class CloseExpiredJobs implements StateSpecificCommandHandler {
	public CommandName = 'Close All Expired Jobs';
	public CommandDescription = 'Closes all expired jobs';
	public Internal = true;
	public Command = ['caej', 'closeallexpiredjobs', 'closeallexpiredgames'];
	public Callback = async (messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		await SOAPUtil.CloseExpiredJobs();
		return messageRoot.reply(`Successfully closed all expired jobs.`);
	};
}

export = new CloseExpiredJobs();
