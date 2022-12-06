export class TransportException extends Error {
	public NetworkExpetion = true;

	public constructor(message: string) {
		super(message);
	}
}
