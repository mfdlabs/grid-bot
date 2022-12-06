import { Message } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { UserUtil } from '../Utility/UserUtility';
import { RccCommandsUtil } from '../RCCService/RccCommandsUtil';
import { AdminUtil } from '../Administration/AdminUtil';

class Render implements StateSpecificCommandHandler {
	public CommandName = 'Render Task';
	public CommandDescription = 'Renders a user.';
	public Internal = !GlobalConfig.RenderEnabled;
	public Command = ['r', 'render', 'sexually-weird-render'];
	public Callback = async (messageContent: string[], messageRoot: Message, originalCommandName: string) => {
		if (!GlobalConfig.RenderEnabled) {
			if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;
		}

		if (originalCommandName === 'sexually-weird-render') {
			try {
				messageRoot.reply({ files: [await RccCommandsUtil.Render(-1, 2537274968, 1068, 1068)] });
			} catch (e) {
				if (e.RccException === true) {
					return messageRoot.reply(e.message);
				} else if (e.NetworkExpetion === true) return messageRoot.reply(e.message);
			}
			return;
		}

		let userID = parseInt(messageContent[0], 10);
		let userName = null;

		if (isNaN(userID)) {
			const e = messageContent.join(' ');
			userName = e;
			if (userName.length > 0) {
				userID = await UserUtil.GetUserIDByUsername(userName);
			} else {
				return messageRoot.reply(
					`Missing required parameter 'userID' or 'userName', the layout is: ${GlobalConfig.Prefix}render userID|userName`,
				);
			}
		} else {
			if (userID > 214748364887) return messageRoot.reply(`The userId '${userID}' is too big.`);
		}

		if (await UserUtil.CheckIsUserBanned(userID))
			return messageRoot.reply(`The user '${userID ?? userName}' is banned or does not exist.`);

		messageRoot.reply({ files: [await RccCommandsUtil.Render(userID, 2537274968, 1068, 1068)] });
	};
}

export = new Render();
