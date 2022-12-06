import { Message } from 'discord.js';
import { Help } from '../Handlers/Help';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class PrefixCheck implements StateSpecificCommandHandler {
	public CommandName = 'Prefix';
	public CommandDescription = 'Checks the current ENV prefix';
	public Command = ['prefix', 'pr'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		return Help.HandlePrefixCheck(messageRoot.channel);
	};
}

export = new PrefixCheck();
