export abstract class GridCommand {
	public abstract Mode: string;

	public abstract MessageVersion?: int;

	public abstract Settings: any;
}
