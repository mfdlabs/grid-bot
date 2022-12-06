import { SOAPUtil } from './SOAPUtility';
import { ScriptExecution } from './Types/SOAPTypes/ScriptExecution';
import { Job } from './Types/SOAPTypes/Job';
import { GameServerCommand } from './Types/GameServerCommand';
import { GameServerSettings } from './Types/GameServerSettings';
import { ThumbnailCommandType } from './Types/ThumbnailCommandType';
import { ThumbnailCommand } from './Types/ThumbnailCommand';
import { ThumbnailSettings } from './Types/ThumbnailSettings';
import { writeFileSync as WriteDataToFile } from 'fs';
import { GlobalConfig } from '../Utility/GlobalConfig';
import { NetUtil } from '../Utility/NetUtility';

export class RccCommandsUtil {
	public static async Render(userId: long, placeID: long, sizeX: long, sizeY: long) {
		let url = `https://avatar.sitetest4.robloxlabs.com/v1/avatar-fetch?userId=${isNaN(userId) ? 1 : userId}`;

		const result = await SOAPUtil.BatchJob(
			new Job(NetUtil.GenerateUUIDV4(), 7, undefined, undefined),
			new ScriptExecution(
				NetUtil.GenerateUUIDV4(),
				RccCommandsUtil.ConstructRenderThumbScript(
					Math.floor(Math.random() * 10) < 6 ? ThumbnailCommandType.Avatar_R15_Action : ThumbnailCommandType.Closeup,
					['https://assetdelivery.roblox.com/v1', `${url}&amp;placeID=${placeID}`, 'PNG', sizeX, sizeY, true, 30, 100, 0, 0],
				),
				undefined,
			),
		);

		const imageBase64 = result[0].value;
		const fileName = `${GlobalConfig.Root}\\Thumb.png`;

		WriteDataToFile(fileName, imageBase64, { encoding: 'base64' });

		return fileName;
	}

	public static async LaunchSimpleGame(jobId: string, placeId: long, gameId: long) {
		return await SOAPUtil.OpenJob(
			new Job(jobId, 20000, undefined, undefined),
			new ScriptExecution(
				'Execute Script',
				RccCommandsUtil.ContructOpenGameServerCommand(
					placeId,
					gameId,
					1,
					`${jobId}-Sig2`,
					null,
					'sitetest4.robloxlabs.com',
					jobId,
					NetUtil.GetLocalIP(),
					1,
					200,
					10,
					1,
					NetUtil.GenerateUUIDV4(),
					1,
					null,
					200,
					1,
					'User',
					1,
					null,
					`https://assetdelivery.sitetest4.robloxlabs.com/v1/asset?id=${placeId}`,
					null,
				),
				undefined,
			),
		);
	}

	private static ConstructRenderThumbScript(type: ThumbnailCommandType, args: any[]) {
		return new ThumbnailCommand(new ThumbnailSettings(type, args));
	}

	private static ContructOpenGameServerCommand(
		placeId: long,
		universeId: long,
		matchmakingId: int,
		jobSignature: string,
		gameCode: string,
		baseUrl: string,
		gameId: string,
		machineAddress: string,
		serverId: int,
		gsmInterval: int,
		maxPlayers: int,
		maxGameInstances: int,
		apiKey: string,
		preferredPlayerCapacity: int,
		placeVisitAccessKey: string,
		datacenterId: int,
		creatorId: long,
		creatorType: string,
		placeVersion: int,
		vipOwnerId: long,
		placeFetchUrl: string,
		metadata: string,
	) {
		return new GameServerCommand(
			new GameServerSettings(
				placeId,
				universeId,
				matchmakingId,
				jobSignature,
				gameCode,
				baseUrl,
				gameId,
				machineAddress,
				serverId,
				gsmInterval,
				maxPlayers,
				maxGameInstances,
				apiKey,
				preferredPlayerCapacity,
				placeVisitAccessKey,
				datacenterId,
				creatorId,
				creatorType,
				placeVersion,
				vipOwnerId,
				placeFetchUrl,
				metadata,
			),
		);
	}
}
