export class Job {
	public id: string;

	public expirationInSeconds: double;

	public category: int;

	public cores: double;

	public constructor(id: string, expirationInSeconds: double, category: int, cores: double) {
		this.id = id;
		this.expirationInSeconds = expirationInSeconds;
		this.category = category;
		this.cores = cores;
	}
}
