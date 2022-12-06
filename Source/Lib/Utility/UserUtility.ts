import http from 'axios';
import { Logger } from '../Logging/LoggingUtility';

export class UserUtil {
	public static async CheckIsUserBanned(userID: long): Promise<bool> {
		const discriminator = `UserUtil_CheckIsUserBanned:${userID}`;
		const [cached, cachedValue] = UserUtil.CheckCacheV2(discriminator);
		if (cached) return cachedValue;

		return await UserUtil.GetUserAvatarFetch(userID);
	}

	private static CheckCacheV2(discriminator: string): [bool, bool] {
		Logger.Info('Try get %s from BanCacheStore', discriminator);

		if (UserUtil.BanCacheStore.has(discriminator)) {
			Logger.Info('Got %s from BanCacheStore, %s', discriminator, UserUtil.BanCacheStore.get(discriminator));

			return [true, UserUtil.BanCacheStore.get(discriminator)];
		}

		Logger.Info('%s was not in BanCacheStore', discriminator);
		return [false, null];
	}

	public static async GetUserIDByUsername(username: string): Promise<long> {
		username = username.toLowerCase().trim();
		const discriminator = `UserUtil_GetUserIDByUsername:${username}`;
		const [cached, cachedValue] = UserUtil.CheckCache(discriminator);

		if (cached) return cachedValue;

		return await UserUtil.GetUserIDByUsernameAsync(username);
	}

	private static async GetUserAvatarFetch(userID: long) {
		try {
			const response = await http.request({ url: `https://avatar.roblox.com/v1/avatar-fetch?userID=${userID}&placeId=1818` });

			const isBanned = UserUtil.ParseResponseFromAvatarFetchRequest(response);

			UserUtil.BanCacheStore.set(`UserUtil_CheckIsUserBanned:${userID}`, isBanned);

			return isBanned;
		} catch (e) {
			Logger.Error(
				'Error occurred with with dispatching of request, status code: %d, response data: %s, stacK trace: %s',
				e.response ? e.response.status : 'Unknown',
				e.response
					? e.response.data instanceof Object
						? JSON.stringify(e.response.data).split('\n').join('\\n')
						: e.response.data.split('\n').join('\\n')
					: 'No response',
				e.stack,
			);
			UserUtil.BanCacheStore.set(`UserUtil_CheckIsUserBanned:${userID}`, true);
			return true;
		}
	}

	private static async GetUserIDByUsernameAsync(username: string) {
		try {
			const response = await http.request({ url: `https://api.roblox.com/users/get-by-username?username=${username}` });

			const userId = UserUtil.ParseResponseFromGetByUsernameRequest(response.data);

			UserUtil.CacheStore.set(`UserUtil_GetUserIDByUsername:${username}`, userId);

			return userId;
		} catch (e) {
			Logger.Error(
				'Error occurred with with dispatching of request, status code: %d, response data: %s, stacK trace: %s',
				e.response ? e.response.status : 'Unknown',
				e.response
					? e.response.data instanceof Object
						? JSON.stringify(e.response.data).split('\n').join('\\n')
						: e.response.data.split('\n').join('\\n')
					: 'No response',
				e.stack,
			);
			UserUtil.CacheStore.set(`UserUtil_GetUserIDByUsername:${username}`, null);
			return null;
		}
	}

	private static ParseResponseFromGetByUsernameRequest(response: any) {
		if (response['success'] === false) return null;

		return response['Id'];
	}

	private static ParseResponseFromAvatarFetchRequest(response: any) {
		if (response.status === 200) return false;

		return true;
	}

	private static CheckCache(discriminator: string): [bool, long] {
		Logger.Info('Try get %s from CacheStore', discriminator);

		if (UserUtil.CacheStore.has(discriminator)) {
			Logger.Info('Got %s from CacheStore, %s', discriminator, UserUtil.CacheStore.get(discriminator));

			return [true, UserUtil.CacheStore.get(discriminator)];
		}

		Logger.Info('%s was not in CacheStore', discriminator);
		return [false, null];
	}

	private static CacheStore: Map<string, long> = new Map<string, long>();
	private static BanCacheStore: Map<string, bool> = new Map<string, bool>();
}
