import { networkInterfaces as GetNetworkInterfaces, hostname as GetMachineHost } from 'os';

export class NetUtil {
	public static GenerateUUIDV4() {
		return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
			var r = (Math.random() * 16) | 0,
				v = c == 'x' ? r : (r & 0x3) | 0x8;
			return v.toString(16);
		});
	}

	public static GetLocalIP() {
		var netInterfaces = GetNetworkInterfaces();
		for (var devName in netInterfaces) {
			var netInterface = netInterfaces[devName];

			for (var i = 0; i < netInterface.length; i++) {
				var alias = netInterface[i];
				if (alias.family === 'IPv4' && alias.address !== '127.0.0.1' && !alias.internal) return alias.address;
			}
		}
		return '0.0.0.0';
	}

	public static GetMachineID() {
		return GetMachineHost();
	}
}
