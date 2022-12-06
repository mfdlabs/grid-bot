import { Message } from 'discord.js';
import { Logger } from '../Logging/LoggingUtility';
import { ScriptUtil } from '../RCCService/ScriptUtility';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { ScriptExecution } from '../RCCService/Types/SOAPTypes/ScriptExecution';
import { Job } from '../RCCService/Types/SOAPTypes/Job';
import { TextUtil } from '../Utility/TextUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { AdminUtil } from '../Administration/AdminUtil';

class ExecuteScript implements StateSpecificCommandHandler {
	public CommandName = 'Exeute Script';
	public CommandDescription = 'Executes the given script.';
	public Internal = !GlobalConfig.ExecuteEnabled;
	public Command = ['x', 'ex', 'execute'];
	public Callback = async (messageContent: string[], messageRoot: Message) => {
		if (!GlobalConfig.ExecuteEnabled) {
			if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;
		}

		Logger.Info(`Execute script from '%s'`, messageRoot.author.id);

		const script = messageContent
			.join(' ')
			.split(/["“‘”]/g)
			.join('"')
			.split('`')
			.join('');
		if (ScriptUtil.CheckIfBlank(script, messageRoot)) return;
		if (ScriptUtil.CheckIfScriptContainsDisallowedText(script, messageRoot)) return;

		if (TextUtil.ContainsUnicode(script)) return messageRoot.reply('Unicode is not supported.');

		let scriptEx = new ScriptExecution(
			'Test',
			{
				Mode: 'ExecuteScript',
				Settings: {
					Type: 'run',
					Arguments: { script: TextUtil.EscapeString(script) },
				},
			},
			undefined,
		);
		try {
			let job = new Job('Test', 200000, undefined, undefined);
			ScriptUtil.ParseLuaValuesAndRespond(await SOAPUtil.OpenJob(job, scriptEx), messageRoot);
			await SOAPUtil.CloseJob('Test');
		} catch (e) {
			if (e.RccException === true) {
				return messageRoot.reply(e.message);
			} else if (e.NetworkExpetion === true) return messageRoot.reply(e.message);
		}
	};
}

export = new ExecuteScript();
