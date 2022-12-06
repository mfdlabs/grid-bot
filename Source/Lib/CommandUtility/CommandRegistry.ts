import { StateSpecificCommandHandler } from './Types/StateSpecificCommandHandler';
import filestream from 'fs';
import { Message, MessageEmbed } from 'discord.js';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { AdminUtil } from '../Administration/AdminUtil';
import { Logger } from '../Logging/LoggingUtility';
import { NetUtil } from '../Utility/NetUtility';
import { TextUtil } from '../Utility/TextUtility';

export class CommandRegistry {
	private static WasRegistered: bool = false;
	private static RegistrationRunning: bool = false;
	public static StateSpecificCommands: StateSpecificCommandHandler[] = [];

	private static GetCommandDirectory() {
		return __dirname + `\\..\\Commands`;
	}

	public static async ConstructHelpEmbedForSingleCommand(commandName: string, authorId: string): Promise<MessageEmbed> {
		if (!CommandRegistry.WasRegistered) await CommandRegistry.RegisterOnce();

		const command = await CommandRegistry.GetCommandByName(commandName);

		if (command === null) {
			return null;
		}

		if (command.Internal && !AdminUtil.CheckIsUserInAdminWhiteList(authorId)) return null;

		const embed = new MessageEmbed();

		embed
			.setColor('#0099ff')
			.setTitle(`${command.CommandName} documentation.`)
			.addField(
				typeof command.Command === 'string' ? command.Command : command.Command.join(', '),
				`${command.CommandDescription} ${command.Internal ? '**INTERNAL**' : ''}`,
				false,
			)
			.setTimestamp();

		return embed;
	}

	public static async ConstructHelpEmbedForCommands(authorId: string): Promise<MessageEmbed> {
		return new Promise(async (resumeFunction) => {
			if (!CommandRegistry.WasRegistered) await CommandRegistry.RegisterOnce();

			const embed = new MessageEmbed();

			CommandRegistry.StateSpecificCommands.forEach((command) => {
				if (command.Internal && !AdminUtil.CheckIsUserInAdminWhiteList(authorId)) return;

				embed.addField(
					`${command.CommandName}: ${typeof command.Command === 'string' ? command.Command : command.Command.join(', ')}`,
					`${command.CommandDescription} ${command.Internal ? '**INTERNAL**' : ''}`,
					false,
				);
			});

			embed.setColor('#0099ff').setTitle(`Documentation.`).setTimestamp();

			return resumeFunction(embed);
		});
	}

	public static async CheckAndRunCommand(commandName: string, messageContent: string[], message: Message): Promise<void> {
		CommandRegistry.InsertIntoAverages(message.channel.id, message.guild.id, message.author.id, commandName);
		CommandRegistry.MetricsCounters.RequestCountN++;
		Logger.Debug(
			"Try execute command '%s' with args '%s' from '%s' (%s) in server '%s' (%s) - channel '%s'",
			TextUtil.EscapeString(commandName),
			messageContent.length > 0 ? TextUtil.EscapeString(messageContent.join(' ').split('\n').join('\\n')) : 'No command arguments.',
			TextUtil.EscapeString(message.author.tag),
			message.author.id,
			message.guild ? TextUtil.EscapeString(message.guild.name) : 'Direct Message',
			message.guild ? message.guild.id : message.channel.id,
			message.channel.id,
		);
		const time = Date.now();

		try {
			if (!CommandRegistry.WasRegistered) await CommandRegistry.RegisterOnce();

			const command = await CommandRegistry.GetCommandByName(commandName);

			if (command === null) {
				CommandRegistry.MetricsCounters.RequestFailedCountN++;
				Logger.Warn("The command '%s', did not exist.", commandName);
				if (GlobalConfig.IsAllowedToEchoBackNotFoundCommandException) {
					message.reply(`The command with the name '${commandName}' was not found.`);
				}
				return;
			}

			await command.Callback(messageContent, message, commandName);

			CommandRegistry.MetricsCounters.RequestSucceededCountN++;

			Logger.Debug("Took %fs time to execute command '%s'.", (Date.now() - time) / 1000, commandName);
		} catch (e) {
			Logger.Debug("Took %fs time to execute command '%s'.", (Date.now() - time) / 1000, commandName);
			CommandRegistry.MetricsCounters.RequestFailedCountN++;

			Logger.Error(`[EID-${NetUtil.GenerateUUIDV4()}] An unexpected error occurred, ${e.stack}`);
			if (e.RccException === true) {
				message.reply(e.message);
				return;
			}
			message.channel.send(
				`<@!${
					message.author.id
				}>, An unexpected Error has occurred. Error ID: ${NetUtil.GenerateUUIDV4()}, send this to <@!360078081224081409>`,
			);
			return;
		}

		return;
	}

	private static async GetCommandByName(commandName: string): Promise<StateSpecificCommandHandler> {
		return new Promise((resumeFunction) => {
			let isLookingThroughArray = false;
			CommandRegistry.StateSpecificCommands.forEach((command, index) => {
				if (typeof command.Command === 'string') {
					if (command.Command === commandName) {
						return resumeFunction(command);
					}
				} else if (Array.isArray(command.Command)) {
					isLookingThroughArray = true;
					command.Command.forEach((cName, i) => {
						isLookingThroughArray = true;
						if (commandName === cName) return resumeFunction(command);

						if (i === command.Command.length - 1) {
							isLookingThroughArray = false;
						}
					});
				}
				if (index === CommandRegistry.StateSpecificCommands.length - 1) {
					setTimeout(() => {
						if (!isLookingThroughArray) return resumeFunction(null);
					}, 50);
				}
			});
		});
	}

	/**
	 * WARNING: THIS DOES NOT RECURSE.
	 */
	private static ParseAndInsertToRegistry(): Promise<void> {
		return new Promise((resumeFunction) => {
			const dir = CommandRegistry.GetCommandDirectory();

			const files = filestream.readdirSync(dir);

			files.forEach((file, index) => {
				if (file.match(/^.*\.(js|jS|JS|Js)$/) !== null) {
					const data = require(dir + '\\' + file);

					try {
						const json = <StateSpecificCommandHandler>data;

						if (typeof json.Command !== 'string') {
							if (!Array.isArray(json.Command)) {
								Logger.Trace(
									"Exception when reading %s: Expected field 'Command' to be of type 'string' or 'string[]', got '%s'",
									file,
									typeof json.Command,
								);
								return;
							}
						}

						if (typeof json.Callback !== 'function') {
							Logger.Trace(
								`Exception when reading %s: Expected field 'Callback' to be of type 'function', got '%s'`,
								file,
								typeof json.Callback,
							);
							return;
						}

						if (json.CommandName) {
							if (typeof json.CommandName !== 'string') {
								Logger.Trace(
									`Exception when reading %s: Expected field 'CommandName' to be of type 'string', got '%s'`,
									file,
									typeof json.CommandName,
								);
							}
						}

						if (json.CommandDescription) {
							if (typeof json.CommandDescription !== 'string') {
								Logger.Trace(
									`Exception when reading %s: Expected field 'CommandDescription' to be of type 'string', got '%s'`,
									file,
									typeof json.CommandDescription,
								);
							}
						}

						CommandRegistry.StateSpecificCommands.push(json);
					} catch (err) {
						Logger.Error(`Exception when reading %s: %s`, file, err.message);
						return;
					}
				}
				if (index === files.length - 1) return resumeFunction();
			});
		});
	}

	public static async RegisterOnce() {
		if (!CommandRegistry.RegistrationRunning && !CommandRegistry.WasRegistered) {
			CommandRegistry.RegistrationRunning = true;
			await CommandRegistry.ParseAndInsertToRegistry();
			CommandRegistry.WasRegistered = true;
			CommandRegistry.RegistrationRunning = false;
		}
	}

	public static LogReport() {
		Logger.Warn('Command Registry Metrics Report for Date (%s, %s)', new Date(Date.now()).toUTCString(), process.uptime().toFixed(7));
		Logger.Log('=====================================================================================');
		Logger.Log('Total command request count: %d', CommandRegistry.MetricsCounters.RequestCountN);
		Logger.Log('Total succeeded command request count: %d', CommandRegistry.MetricsCounters.RequestSucceededCountN);
		Logger.Log('Total failed command request count: %d', CommandRegistry.MetricsCounters.RequestFailedCountN);

		const averages = CommandRegistry.CalculateAverages();

		Logger.Log("Average request channel: '%s' with average of %d", averages.Channels['Item'], averages.Channels['Average']);
		Logger.Log("Average request guild: '%s' with average of %d", averages.Servers['Item'], averages.Servers['Average']);
		Logger.Log("Average request user: '%s' with average of %d", averages.Users['Item'], averages.Users['Average']);
		Logger.Log("Average request command name: '%s' with average of %d", averages.Commands['Item'], averages.Commands['Average']);

		Logger.Log('=====================================================================================');
	}

	private static InsertIntoAverages(channelId: string, serverId: string, userId: string, commandName: string) {
		CommandRegistry.Averages.Channels.push(channelId);
		CommandRegistry.Averages.Servers.push(serverId);
		CommandRegistry.Averages.Users.push(userId);
		CommandRegistry.Averages.Commands.push(commandName);
	}
	private static CalculateAverages() {
		return {
			Channels: CommandRegistry.CalculateModeOfArray(CommandRegistry.Averages.Channels),
			Servers: CommandRegistry.CalculateModeOfArray(CommandRegistry.Averages.Servers),
			Users: CommandRegistry.CalculateModeOfArray(CommandRegistry.Averages.Users),
			Commands: CommandRegistry.CalculateModeOfArray(CommandRegistry.Averages.Commands),
		};
	}

	private static readonly MetricsCounters = {
		RequestCountN: 0,
		RequestFailedCountN: 0,
		RequestSucceededCountN: 0,
	};

	private static readonly Averages = {
		Channels: [] as string[],
		Servers: [] as string[],
		Users: [] as string[],
		Commands: [] as string[],
	};

	private static CalculateModeOfArray<T>(array: T[]) {
		if (array === null || array.length === 0)
			return {
				Item: 'No requests',
				Average: 0,
			};
		var mf = 1;
		var m = 0;
		var item: T;
		for (var i = 0; i < array.length; i++) {
			for (var j = i; j < array.length; j++) {
				if (array[i] == array[j]) m++;
				if (mf < m) {
					mf = m;
					item = array[i];
				}
			}
			m = 0;
		}

		return { Item: item === undefined ? array[array.length - 1] : item, Average: mf };
	}
}
