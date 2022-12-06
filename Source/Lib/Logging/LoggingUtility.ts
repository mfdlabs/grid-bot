import { format as FormatString } from 'util';
import {
	mkdirSync as CreateDirectory,
	appendFileSync as AppendStringToFile,
	existsSync as CheckDoesFileOrFolderExist,
	rmSync as RemoveDirectory,
} from 'fs';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { NetUtil } from '../Utility/NetUtility';

export class Logger {
	private static ConstructLoggerMessage(type: string, message: string, ...args: any[]) {
		return FormatString(
			`[${new Date(Date.now()).toISOString()}][${process.uptime().toFixed(7)}][${process.pid.toString(16)}][${process.platform}-${
				process.arch
			}][${process.version}][${NetUtil.GetLocalIP()}][${NetUtil.GetMachineID()}][bot][${type.toUpperCase()}] ${message}\n`,
			...args,
		);
	}

	private static LogLocally(type: string, message: string, ...args: any[]) {
		const str = Logger.ConstructLoggerMessage(type, message, ...args);
		const dirName = `${process.env.LOCALAPPDATA}\\RccService\\Logs`;
		if (!CheckDoesFileOrFolderExist(dirName + '\\..\\')) CreateDirectory(dirName + '\\..\\');
		if (!CheckDoesFileOrFolderExist(dirName)) CreateDirectory(dirName);

		AppendStringToFile(dirName + `\\log_${process.pid.toString(16).toUpperCase()}.log`, str);
	}

	public static TryClearLogs(overrideGlobalConfig: bool = false) {
		Logger.Log('Try clear logs.');

		if (GlobalConfig.PersistLocalLogs) {
			if (overrideGlobalConfig) {
				Logger.Warn('Overriding global config when clearing logs.');
			} else {
				Logger.Warn('The local log is set to persist. Please change ENVVAR LOG_PERSIST to change this.');
				return;
			}
		}

		Logger.Log('Clearing LocalLog...');

		const dirName = `${process.env.LOCALAPPDATA}\\RccService\\Logs`;
		if (CheckDoesFileOrFolderExist(dirName)) {
			RemoveDirectory(dirName, { recursive: true, force: true });
			return;
		}
	}

	public static Log(message: string, ...args: any[]) {
		console.log(`${Logger.GetSharedColorString()}[\x1b[97mLOG\x1b[0m] \x1b[97m${message}\x1b[0m`, ...args);

		Logger.LogLocally('LOG', message, ...args);
	}

	public static Warn(message: string, ...args: any[]) {
		console.log(`${Logger.GetSharedColorString()}[\x1b[93mWARN\x1b[0m] \x1b[93m${message}\x1b[0m`, ...args);

		Logger.LogLocally('WARN', message, ...args);
	}

	public static Trace(message: string, ...args: any[]) {
		console.trace(`${Logger.GetSharedColorString()}[\x1b[91mTRACE\x1b[0m] \x1b[91m${message}\x1b[0m`, ...args);

		Logger.LogLocally('TRACE', message, ...args);
	}

	public static Debug(message: string, ...args: any[]) {
		console.log(`${Logger.GetSharedColorString()}[\x1b[95mDEBUG\x1b[0m] \x1b[95m${message}\x1b[0m`, ...args);

		Logger.LogLocally('DEBUG', message, ...args);
	}

	public static Info(message: string, ...args: any[]) {
		console.log(`${Logger.GetSharedColorString()}[\x1b[94mINFO\x1b[0m] \x1b[94m${message}\x1b[0m`, ...args);

		Logger.LogLocally('INFO', message, ...args);
	}

	public static Error(message: string, ...args: any[]) {
		console.log(`${Logger.GetSharedColorString()}[\x1b[91mERROR\x1b[0m] \x1b[91m${message}\x1b[0m`, ...args);

		Logger.LogLocally('ERROR', message, ...args);
	}

	private static GetSharedColorString() {
		return `[\x1b[90m${new Date(Date.now()).toISOString()}\x1b[0m][\x1b[90m${process
			.uptime()
			.toFixed(7)}\x1b[0m][\x1b[90m${process.pid.toString(16)}\x1b[0m][\x1b[90m${process.platform}-${process.arch}\x1b[0m][\x1b[90m${
			process.version
		}\x1b[0m][\x1b[90m${NetUtil.GetLocalIP()}\x1b[0m][\x1b[90m${NetUtil.GetMachineID()}\x1b[0m][bot]`;
	}
}
