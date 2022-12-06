import { GlobalConfig } from '../Utility/GlobalConfig';
import { Logger } from '../Logging/LoggingUtility';

export class Login {
	public static async OnLogin() {
		Logger.Debug("Logged in as '%s'", GlobalConfig.Bot.user.tag);
		await GlobalConfig.Bot.user.setStatus('idle');
		await GlobalConfig.Bot.user.setActivity({ name: 'Roblox', url: 'https://www.roblox.com', type: 'COMPETING' });
	}
}
