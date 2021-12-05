using System;

namespace MFDLabs.EventLog
{
    public static class StaticLoggerRegistry
    {
        public static ILogger Instance
        {
            get
            {
                if (_Instance == null) throw new UninitializedLoggerException();
                return _Instance;
            }
            private set => _Instance = value;
        }
        public static bool HasLogger => _Instance != null;

        public static void SetLogger(ILogger logger) 
            => Instance = logger ?? throw new ArgumentNullException(nameof(logger));

        private static ILogger _Instance;
    }
}
