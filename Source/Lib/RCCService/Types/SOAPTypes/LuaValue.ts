import { LuaType } from './LuaType';

export class LuaValue {
	public type: LuaType;

	public value: string;

	public table: LuaValue[];

	public constructor(type: LuaType, value: string, table: LuaValue[]) {
		this.type = type;
		this.value = value;
		this.table = table;
	}
}
