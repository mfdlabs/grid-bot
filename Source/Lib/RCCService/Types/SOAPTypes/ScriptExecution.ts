import { GridCommand } from '../GridCommand';
import { LuaValue } from './LuaValue';

export class ScriptExecution {
	public name: string;

	public script: string;

	public arguments: LuaValue[];

	public constructor(name: string, script: GridCommand, args: LuaValue[]) {
		this.name = name;
		this.script = JSON.stringify(script);
		this.arguments = args;
	}
}
