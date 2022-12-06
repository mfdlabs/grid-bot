import { GridCommand } from './GridCommand';
import { ThumbnailSettings } from './ThumbnailSettings';

export class ThumbnailCommand extends GridCommand {
	public Mode: string = 'Thumbnail';
	public MessageVersion: number = 1;
	public Settings: ThumbnailSettings;

	public constructor(settings: ThumbnailSettings) {
		super();
		this.Settings = settings;
	}
}
