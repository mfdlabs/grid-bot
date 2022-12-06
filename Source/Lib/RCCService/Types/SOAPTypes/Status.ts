export class Status {
	public version: string;

	public environmentCount: int;

	public constructor(version: string, environmentCount: int) {
		this.version = version;
		this.environmentCount = environmentCount;
	}
}
