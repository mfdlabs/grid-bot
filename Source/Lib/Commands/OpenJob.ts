import { Message } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { AdminUtil } from '../Administration/AdminUtil';
import { ConversionUtil } from '../Utility/ConversionUtil';
import { RccCommandsUtil } from '../RCCService/RccCommandsUtil';
import { ScriptUtil } from '../RCCService/ScriptUtility';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';

class OpenJob implements StateSpecificCommandHandler {
	public CommandName = 'Open Job';
	public CommandDescription = 'Opens a job with the parameters of jobID, placeId and gameID';
	public Internal = true;
	public Command = ['oj', 'openjob', 'opengame'];
	public Callback = async (messageContent: string[], messageRoot: Message) => {
		if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;

		const jobId = messageContent[0];
		let placeId = ConversionUtil.ToInt64(messageContent[1]);
		let universeId = ConversionUtil.ToInt64(messageContent[2]);

		if (!jobId)
			return messageRoot.reply(
				`Missing required parameter 'jobId', the layout is: ${GlobalConfig.Prefix}openjob jobID placeID?=1818 universeID?=1`,
			);

		if (isNaN(placeId)) placeId = 1818;
		if (isNaN(universeId)) universeId = 1;

		return ScriptUtil.ParseLuaValuesAndRespond(await RccCommandsUtil.LaunchSimpleGame(jobId, placeId, universeId), messageRoot);
	};
}

export = new OpenJob();
