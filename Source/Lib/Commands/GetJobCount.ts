import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class GetJobCount implements StateSpecificCommandHandler {
	public CommandName = 'Get Job Count';
	public CommandDescription = 'Gets all job count';
	public Internal = true;
	public Command = ['gjc', 'getjobcount', 'getgamecount'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		const jobs = await SOAPUtil.GetAllJobs();
		return messageRoot.reply(
			jobs === undefined ? 'There are currently no jobs open.' : `Job Count: ${Array.isArray(jobs) ? jobs.length : 1}`,
		);
	};
}

export = new GetJobCount();
