import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class GetVersion implements StateSpecificCommandHandler {
	public CommandName = 'GetVersion';
	public CommandDescription =
		'Invokes a GetVersion request to the RCCService instance, if GlobalENV::LaunchRCCIfNotOpen is not set it will fail.';

	public Internal = true;
	public Command = ['gv', 'getversion'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		messageRoot.reply(await SOAPUtil.GetVersion());
	};
}

export = new GetVersion();
