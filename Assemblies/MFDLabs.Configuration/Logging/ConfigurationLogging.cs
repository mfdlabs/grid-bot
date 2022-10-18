using System;
using MFDLabs.Logging;

namespace MFDLabs.Configuration.Logging
{
	public static class ConfigurationLogging
	{
		public static void OverrideDefaultConfigurationLogging(Action<string, object[]> onError,
			Action<string, object[]> onWarning,
			Action<string, object[]> onInformation)
		{
			_overrideOnError = onError;
			_overrideOnWarning = onWarning;
			_overrideOnInformation = onInformation;
		}
        internal static void Error(string format, params object[] args) => Log(_overrideOnError, format, args);
        internal static void Warning(string format, params object[] args) => Log(_overrideOnWarning, format, args);
        internal static void Info(string format, params object[] args) => Log(_overrideOnInformation, format, args);
        private static void Log(Action<string, object[]> overrideLogger, string format, params object[] args) => overrideLogger?.Invoke(format, args);

        private static Action<string, object[]> _overrideOnError = (f, a) => Logger.Singleton.Error(f, a);
		private static Action<string, object[]> _overrideOnWarning = (f, a) => Logger.Singleton.Warning(f, a);
		private static Action<string, object[]> _overrideOnInformation = (f, a) => Logger.Singleton.Info(f, a);
	}
}
