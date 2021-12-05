using System;

namespace MFDLabs.Configuration.Logging
{
	public static class ConfigurationLogging
	{
		public static void OverrideDefaultConfigurationLogging(Action<string, object[]> onError, Action<string, object[]> onWarning, Action<string, object[]> onInformation)
		{
			_OverrideOnError = onError;
			_OverrideOnWarning = onWarning;
			_OverrideOnInformation = onInformation;
		}
        internal static void Error(string format, params object[] args) => Log(_OverrideOnError, format, args);
        internal static void Warning(string format, params object[] args) => Log(_OverrideOnWarning, format, args);
        internal static void Info(string format, params object[] args) => Log(_OverrideOnInformation, format, args);
        private static void Log(Action<string, object[]> overrideLogger, string format, params object[] args) => overrideLogger?.Invoke(format, args);

        private static Action<string, object[]> _OverrideOnError;
		private static Action<string, object[]> _OverrideOnWarning;
		private static Action<string, object[]> _OverrideOnInformation;
	}
}
