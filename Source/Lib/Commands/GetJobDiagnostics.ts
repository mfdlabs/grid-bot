import { Message } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { AdminUtil } from '../Administration/AdminUtil';
import { ConversionUtil } from '../Utility/ConversionUtil';
import { ScriptUtil } from '../RCCService/ScriptUtility';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class GetJobDiagnostics implements StateSpecificCommandHandler {
	public CommandName = 'Get Job Diagnostcs';
	public CommandDescription = 'Gets diagnostics for a job.';
	public Internal = true;
	public Command = ['jd', 'jobdiag', 'jobdiagnostics'];
	public Callback = async (messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		const jobId = messageContent[0];
		const type = ConversionUtil.ToInt32(messageContent[1]) || 1;

		if (!jobId)
			return messageRoot.reply(
				`Missing required parameter 'jobId', the layout is: ${GlobalConfig.Prefix}jobdiagnostics jobID type?=1`,
			);

		return ScriptUtil.ParseLuaValuesAndRespond(await SOAPUtil.Diag(type, jobId), messageRoot);
	};
}

export = new GetJobDiagnostics();
