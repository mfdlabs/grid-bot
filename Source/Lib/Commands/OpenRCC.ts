import { Message } from 'discord.js';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { AdminUtil } from '../Administration/AdminUtil';

class OpenRCCFrontend implements StateSpecificCommandHandler {
	public CommandName = 'Open RCC';
	public CommandDescription = 'Tried to open RCC if not open already';
	public Internal = true;
	public Command = ['orcc', 'openrccservice'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		SOAPUtil.OpenRCC();
		messageRoot.reply('Successfully opened RCCService!');
	};
}

export = new OpenRCCFrontend();
