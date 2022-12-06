import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { SystemUtil } from '../Utility/SystemUtil';

class KillServer implements StateSpecificCommandHandler {
	public CommandName = 'Kill Server';
	public CommandDescription = 'Kills the running server';
	public Internal = true;
	public Command = ['ksrv', 'killserver'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		SystemUtil.KillServerSafe();
		messageRoot.reply(`Successfully killed Server`);
	};
}

export = new KillServer();
