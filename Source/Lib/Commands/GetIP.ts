import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { NetUtil } from '../Utility/NetUtility';

class GetIP implements StateSpecificCommandHandler {
	public CommandName = 'Get IP address';
	public CommandDescription = 'Gets the IP address.';
	public Internal = true;
	public Command = ['gip', 'getip', 'getipaddress'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		return messageRoot.reply(`Query Request ID: ${NetUtil.GenerateUUIDV4()}: ${NetUtil.GetLocalIP()} - ${NetUtil.GetMachineID()}`);
	};
}

export = new GetIP();
