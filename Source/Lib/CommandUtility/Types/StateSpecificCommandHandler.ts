import { Message } from 'discord.js';

export interface StateSpecificCommandHandler {
	/**
	 * The name of the command to be in the CommandRegistry
	 */
	CommandName?: string;

	/**
	 * The description of the command to be in the CommandRegistry
	 */
	CommandDescription?: string;

	/**
	 * The Command IDS to be registered into the CommandRegistry
	 */
	Command: string | string[];

	/**
	 * The callback to be invoked on command call.
	 * @param messageContent Message parameters
	 * @param message The discord message.
	 */
	Callback(messageContent: string[], messageRoot: Message, originalCommand?: string): Promise<any>;

	/**
	 * If true, only viewable by Bot developers.
	 */
	Internal?: bool;
}
