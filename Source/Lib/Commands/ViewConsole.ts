import { Message } from 'discord.js';
import { StateSpecificCommandHandler } from '../CommandUtility/Types/StateSpecificCommandHandler';
import { unlinkSync } from 'fs';
import { execSync } from 'child_process';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { SOAPUtil } from '../RCCService/SOAPUtility';
import { AdminUtil } from '../Administration/AdminUtil';

class ViewConsole implements StateSpecificCommandHandler {
	public CommandName = 'View Console';
	public CommandDescription = 'View the RCC console';
	public Internal = !GlobalConfig.ViewConsoleEnabled;
	public Command = ['vc', 'viewconsole'];
	public Callback = async (_messageContent: string[], messageRoot: Message) => {
		if (!GlobalConfig.ViewConsoleEnabled) {
			if (!AdminUtil.RejectIfNotAdmin(messageRoot)) return;
		}

		SOAPUtil.OpenRCC();
		setTimeout(() => {
			execSync(GlobalConfig.Root + '\\Bin\\Roblox.Screenshot.Relay.exe');
			messageRoot.channel.send({ files: [GlobalConfig.Root + '\\Windowshot.png'] });

			setTimeout(function () {
				try {
					unlinkSync(GlobalConfig.Root + '\\Windowshot.png');
				} catch {}
			}, 2000);
		}, 1250);
	};
}

export = new ViewConsole();
