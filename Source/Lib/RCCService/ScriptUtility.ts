import { Message } from 'discord.js';
import { Logger } from '../Logging/LoggingUtility';
import { LuaType } from './Types/SOAPTypes/LuaType';
import { LuaValue } from './Types/SOAPTypes/LuaValue';

export class ScriptUtil {
	public static CheckIfScriptContainsDisallowedText(script: string, message: Message) {
		Logger.Info("Check if script '%s' contains blacklisted words.", script.split('\n').join('\\n'));

		if (
			script.includes('HttpGet') ||
			script.includes('HttpPost') ||
			script.includes('fenv') ||
			script.includes('while true do') ||
			script.includes('SetUploadUrl') ||
			script.includes('crash__(') ||
			script.includes('do print("') ||
			script.includes('do print()') ||
			script.includes('math.huge do')
		) {
			Logger.Warn("The script '%s' contains blacklisted words.", script.split('\n').join('\\n'));

			message.reply('**CENSORED CODE DETECTED!** :no_entry: ');
			return true;
		}
		Logger.Warn("The script '%s' does not contain blacklisted words.", script.split('\n').join('\\n'));
		return false;
	}
	public static CheckIfBlank(script: string, message: Message) {
		Logger.Info("Check if script '%s' is blank.", script.split('\n').join('\\n'));
		if (script.length < 1) {
			Logger.Warn("The script '%s' is blank.", script.split('\n').join('\\n'));
			message.reply('The script is actually required by the way.');
			return true;
		}
		Logger.Info("The script '%s' is not blank.", script.split('\n').join('\\n'));

		return false;
	}

	public static LuaTypeToString(type: LuaType) {
		switch (type) {
			case LuaType.LUA_TBOOLEAN:
				return 'bool';
			case LuaType.LUA_TNIL:
				return 'null';
			case LuaType.LUA_TNUMBER:
				return 'int';
			case LuaType.LUA_TSTRING:
				return 'string';
			case LuaType.LUA_TTABLE:
				return 'object';
		}
	}

	public static async ParseLuaArray(table: LuaValue | LuaValue[]): Promise<any[]> {
		return new Promise((resumeFunction) => {
			let response = [];
			if (Array.isArray(table) || table[0] !== undefined)
				(<LuaValue[]>(<unknown>Object.values(table))).forEach(async (v, idx) => {
					if (v.type === <LuaType>(<unknown>'LUA_TTABLE')) v = await ScriptUtil.ParseLuaArray(v)[0];
					response.push(v.value);
					if (idx === (<LuaValue[]>(<unknown>Object.keys(table))).length - 1) return resumeFunction(response);
				});
			else return resumeFunction([table.value]);
		});
	}

	public static ParseLuaValuesAndRespond(vals: LuaValue[], message: Message) {
		if (vals.length === 0) {
			return message.reply('Executed script with no return!');
		}

		let text = ['Return Value:'];

		vals.forEach(async (v, idx) => {
			text.push(
				`${
					(v.type === LuaType.LUA_TSTRING ? `"${v.value}"` : v.value) ||
					(v.table !== undefined && typeof v.table !== 'string'
						? JSON.stringify(await ScriptUtil.ParseLuaArray(v.table['LuaValue']))
						: typeof v.table === 'string'
						? '{}'
						: 'null')
				} (${ScriptUtil.LuaTypeToString(v.type)})${idx === vals.length - 1 ? '' : ','}`,
			);
			if (idx === vals.length - 1) {
				message.reply(text.join(' '));
				return;
			}
		});
	}
}
