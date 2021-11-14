using System;
using MFDLabs.Logging;

namespace MFDLabs.Configuration.Logging
{
	public static class ConfigurationLogging
	{
		public static void OverrideDefaultConfigurationLogging(Action<string, object[]> onError, Action<string, object[]> onWarning, Action<string, object[]> onInformation)
		{
			var useEventLogger = global::MFDLabs.Configuration.Properties.Settings.Default.ConfigurationLoggingUseSystemLogger;
			_OverrideOnError = onError ?? ((m, a) => 
			{ 
				if (useEventLogger) EventLogConsoleSystemLogger.Singleton.Error(m, a);
			});
			_OverrideOnWarning = onWarning ?? ((m, a) =>
			{
				if (useEventLogger) EventLogConsoleSystemLogger.Singleton.Warning(m, a);
			});
			_OverrideOnInformation = onInformation ?? ((m, a) =>
			{
				if (useEventLogger) EventLogConsoleSystemLogger.Singleton.Info(m, a);
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

		private static void Log(Action<string, object[]> overrideLogger, string format, params object[] args)
		{
			overrideLogger?.Invoke(format, args);
		}

		private static Action<string, object[]> _OverrideOnError = (m, a) =>
		{
			EventLogConsoleSystemLogger.Singleton.Error(m, a);
		};

		private static Action<string, object[]> _OverrideOnWarning = (m, a) =>
		{
			EventLogConsoleSystemLogger.Singleton.Warning(m, a);
		};

		private static Action<string, object[]> _OverrideOnInformation = (m, a) =>
		{
			EventLogConsoleSystemLogger.Singleton.Info(m, a);
		};
	}
}
