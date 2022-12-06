import { exec, ExecException, execSync } from 'child_process';
import { Logger } from '../Logging/LoggingUtility';

export class SystemUtil {
	public static CheckIsRunning(query: string, callBack: (isRunning: boolean) => void) {
		let platform = process.platform;
		let command = '';
		switch (platform) {
			case 'win32':
				command = `tasklist`;
				break;
			case 'darwin':
				command = `ps -ax | grep ${query}`;
				break;
			case 'linux':
				command = `ps -A`;
				break;
			default:
				break;
		}
		exec(command, (_error: ExecException, stderr: string) => {
			callBack(stderr.toLowerCase().indexOf(query.toLowerCase()) > -1);
		});
	}

	public static KillRccService() {
		execSync('taskkill /f /t /im rccservice.exe');
	}

	public static KillServer() {
		execSync('taskkill /FI "WindowTitle eq npm run Start-Main-Job" /t /f');
	}

	public static KillServerSafe() {
		Logger.Log('Trying to close backend Server');
		execSync('taskkill /FI "WindowTitle eq npm run Start-Main-Job" /t /f');
		Logger.Info('Successfully closed backed Server.');
	}

	public static KillRccServiceSafe() {
		Logger.Log('Trying to close RCCService');

		try {
			execSync('taskkill /f /t /im rccservice.exe');
			Logger.Info('Successfully closed RCCService.');
		} catch (ex) {
			Logger.Warn('RCCService not running, ignoring...');
		}
	}
}
