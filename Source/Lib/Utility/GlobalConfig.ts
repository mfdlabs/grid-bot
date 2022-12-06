import { Client } from 'discord.js';
import { DotENV } from '../Events/DotENV';
import { ConversionUtil } from './ConversionUtil';

export class GlobalConfig {
	private static _client: Client = null;

	public static Root = `z:\\kairex\\vx\\v1\\old-dev-bot`;

	public static get BotToken(): string {
		DotENV.GlobalConfigure();
		return process.env.TOKEN;
	}

	public static get WhitelistedUsers(): string[] {
		DotENV.GlobalConfigure();
		return process.env.USER_WHITE_LIST ? process.env.USER_WHITE_LIST.split(',') : [];
	}

	public static get IsEnabled(): bool {
		DotENV.GlobalConfigure();
		return ConversionUtil.ToBoolean(process.env.ALLOWED_FOR_PARSING, true);
	}
	public static get AllowParsingForBots(): bool {
		DotENV.GlobalConfigure();
		return ConversionUtil.ToBoolean(process.env.BOTS_ALLOWED, false);
	}

	public static get AllowAllChannels(): bool {
		DotENV.GlobalConfigure();
		return ConversionUtil.ToBoolean(process.env.ALL_CHANNELS_ALLOWED, false);
	}

	public static get WhiteListedChannelIds(): string[] {
		DotENV.GlobalConfigure();
		return process.env.WHITE_LIST ? process.env.WHITE_LIST.split(',') : [];
	}

	public static get ReasonForDying(): string {
		DotENV.GlobalConfigure();
		return process.env.MSG;
	}

	public static get Prefix(): string {
		DotENV.GlobalConfigure();
		return process.env.PREFIX; /* Getter here so it can be dynamic. */
	}

	public static get Bot(): Client {
		return GlobalConfig._client;
	}

	public static set Bot(value: Client) {
		GlobalConfig._client = value;
	}

	public static get IsAllowedToEchoBackNotFoundCommandException(): bool {
		DotENV.GlobalConfigure();
		return ConversionUtil.ToBoolean(process.env.ECHO_BACK_NOT_FOUND, false);
	}

	public static get LaunchRCCIfNotOpen(): bool {
		DotENV.GlobalConfigure();
		return ConversionUtil.ToBoolean(process.env.OPEN_IF_NOT_OPEN, false);
	}

	public static get VerboseErrors(): bool {
		DotENV.GlobalConfigure();
		return ConversionUtil.ToBoolean(process.env.VERBOSE, false);
	}

	public static get PersistLocalLogs(): bool {
		DotENV.GlobalConfigure();
		return ConversionUtil.ToBoolean(process.env.LOG_PERSIST, false);
	}

	public static get ViewConsoleEnabled(): bool {
		DotENV.GlobalConfigure();
		return !ConversionUtil.ToBoolean(process.env.DISABLE_VIEW_CONSOLE, false);
	}

	public static get RenderEnabled(): bool {
		DotENV.GlobalConfigure();
		return !ConversionUtil.ToBoolean(process.env.DISABLE_RENDER, false);
	}

	public static get ExecuteEnabled(): bool {
		DotENV.GlobalConfigure();
		return !ConversionUtil.ToBoolean(process.env.DISABLE_EXECUTE, false);
	}
}
