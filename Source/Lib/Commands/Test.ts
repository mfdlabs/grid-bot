import { Message } from 'discord.js';
import { AdminUtil } from '../Administration/AdminUtil';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { ScriptExecution } from '../RCCService/Types/SOAPTypes/ScriptExecution';
import { Job } from '../RCCService/Types/SOAPTypes/Job';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { ScriptUtil } from '../RCCService/ScriptUtility';

class Test implements StateSpecificCommandHandler {
	public CommandName = 'Test';
	public CommandDescription = 'Funny Test';
	public Command = ['t', 'test'];
	public Callback = async (messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		let job = new Job('Test', 20000, undefined, undefined);
		let script = new ScriptExecution(
			'Test',
			{
				Mode: 'ExecuteScript',
				Settings: {
					Type: 'run',
					Arguments: { script: 'return 1, 2, 3;' },
				},
			},
			undefined,
		);

		let values = await SOAPUtil.OpenJob(job, script);
		await SOAPUtil.CloseJob('Test');

		return ScriptUtil.ParseLuaValuesAndRespond(values, messageRoot);
	};
	public Internal = true;
}

export = new Test();
