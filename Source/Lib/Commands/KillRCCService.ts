import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { SystemUtil } from '../Utility/SystemUtil';

class KillRCCService implements StateSpecificCommandHandler {
	public CommandName = 'Kill RCCService';
	public CommandDescription = 'Kills RCCService';
	public Internal = true;
	public Command = ['krcc', 'killrcc'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		SystemUtil.KillRccServiceSafe();
		messageRoot.reply(`Successfully killed RCCService`);
	};
}

export = new KillRCCService();
