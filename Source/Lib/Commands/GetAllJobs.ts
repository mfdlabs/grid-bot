import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class GetAllJobs implements StateSpecificCommandHandler {
	public CommandName = 'Get All Jobs';
	public CommandDescription = 'Gets all jobs';
	public Internal = true;
	public Command = ['gaj', 'getalljobs', 'getallgames'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		const jobs = JSON.stringify(await SOAPUtil.GetAllJobs());
		return messageRoot.reply(jobs === undefined ? 'There are currently no jobs open.' : jobs, {
			split: { maxLength: 1000, char: ',' },
		});
	};
}

export = new GetAllJobs();
