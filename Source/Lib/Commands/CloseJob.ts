import { Message } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { AdminUtil } from '../Administration/AdminUtil';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class CloseJob implements StateSpecificCommandHandler {
	public CommandName = 'Close Job';
	public CommandDescription = 'Closes a job with the parameters of jobIDD';
	public Internal = true;
	public Command = ['cj', 'closejob', 'closegame'];
	public Callback = async (messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		const jobId = messageContent[0];

		if (!jobId) return messageRoot.reply(`Missing required parameter 'jobId', the layout is: ${GlobalConfig.Prefix}closejob jobID`);

		await SOAPUtil.CloseJob(jobId);
		return messageRoot.reply(`Successfully closed the job '${jobId}'`);
	};
}

export = new CloseJob();
