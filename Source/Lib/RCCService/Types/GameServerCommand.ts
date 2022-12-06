import { GridCommand } from './GridCommand';
import { GameServerSettings } from './GameServerSettings';

export class GameServerCommand extends GridCommand {
	public Mode: string = 'GameServer';
	public MessageVersion: number = 1;
	public Settings: GameServerSettings;

	public constructor(settings: GameServerSettings) {
		super();
		this.Settings = settings;
	}
}
