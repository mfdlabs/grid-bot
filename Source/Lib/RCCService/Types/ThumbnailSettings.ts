import { ThumbnailCommandType } from './ThumbnailCommandType';

export class ThumbnailSettings {
	public Type: ThumbnailCommandType;

	public Arguments: any[];

	public constructor(type: ThumbnailCommandType, args: any[]) {
		this.Type = <ThumbnailCommandType>(<unknown>ThumbnailSettings.ThumbnailCommandTypeToString(type));
		this.Arguments = args;
	}

	private static ThumbnailCommandTypeToString(type: ThumbnailCommandType) {
		return ThumbnailCommandType[type];
	}
}
