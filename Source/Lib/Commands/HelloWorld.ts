import { Message } from 'discord.js';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { AdminUtil } from '../Administration/AdminUtil';

class HelloWorld implements StateSpecificCommandHandler {
	public CommandName = 'Hello World';
	public CommandDescription = 'Invokes a Hello World request.';
	public Internal = true;

	public Command = ['hw', 'helloworld'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		messageRoot.reply(await SOAPUtil.HelloWorld());
	};
}

export = new HelloWorld();
