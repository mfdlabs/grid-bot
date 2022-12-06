import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class GetStatus implements StateSpecificCommandHandler {
	public CommandName = 'GetStatus';
	public CommandDescription =
		'Invokes a GetStatus request to the RCCService instance, if GlobalENV::LaunchRCCIfNotOpen is not set it will fail.';
	public Internal = true;

	public Command = ['gs', 'getstatus'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		messageRoot.reply(JSON.stringify(await SOAPUtil.GetStatus()));
	};
}

export = new GetStatus();
