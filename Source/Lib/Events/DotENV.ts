import { parse } from 'dotenv';
import { readFileSync } from 'fs';
import { GlobalConfig } from '../Utility/GlobalConfig';

export class DotENV {
	public static GlobalConfigure() {
		const data = parse(readFileSync(GlobalConfig.Root + '\\.env'));

		for (const k in data) {
			process.env[k] = data[k];
		}
	}
}
