export class SoapException extends Error {
	public RccException = true;

	public constructor(message: string) {
		super(message);
	}
}
