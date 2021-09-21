using System;
using MFDLabs.Logging;

namespace MFDLabs.Configuration.Logging
{
	public static class ConfigurationLogging
	{
		public static void OverrideDefaultConfigurationLogging(Action<string> onError, Action<string> onWarning, Action<string> onInformation)
		{
			var useEventLogger = global::MFDLabs.Configuration.Properties.Settings.Default.ConfigurationLoggingUseSystemLogger;
			_OverrideOnError = onError ?? ((m) => 
			{ 
				if (useEventLogger) EventLogConsoleSystemLogger.Singleton.Error(m);
			});
			_OverrideOnWarning = onWarning ?? ((m) =>
			{
				if (useEventLogger) EventLogConsoleSystemLogger.Singleton.Warning(m);
			});
			_OverrideOnInformation = onInformation ?? ((m) =>
			{
				if (useEventLogger) EventLogConsoleSystemLogger.Singleton.Info(m);
			});
		}

		internal static void Error(string format, params object[] args)
		{
			Log(_OverrideOnError, format, args);
		}

		internal static void Warning(string format, params object[] args)
		{
			Log(_OverrideOnWarning, format, args);
		}

		internal static void Info(string format, params object[] args)
		{
			Log(_OverrideOnInformation, format, args);
		}

		private static void Log(Action<string> overrideLogger, string format, params object[] args)
		{
			overrideLogger?.Invoke(string.Format(format, args));
		}

		private static Action<string> _OverrideOnError;

		private static Action<string> _OverrideOnWarning;

		private static Action<string> _OverrideOnInformation;
	}
}
