import { Logger } from '../Logging/LoggingUtility';

export class TextUtil {
	public static EscapeString(str: string) {
		return str.replace(/\\u[\dA-F]{4}/gi, function (match) {
			return String.fromCharCode(parseInt(match.replace(/\\u/g, ''), 16));
		});
	}

	public static ContainsUnicode(str: string) {
		Logger.Log("Check if text '%s' contains unicode chars.", str.split('\n').join('\\n'));
		for (var i = 0; i < str.length; i++) {
			if (str.charCodeAt(i) > 127) {
				Logger.Log("The text '%s' contains unicode chars.", str.split('\n').join('\\n'));
				return true;
			}
		}
		Logger.Log("The text '%s' does not contain unicode chars.", str.split('\n').join('\\n'));
		return false;
	}
}
