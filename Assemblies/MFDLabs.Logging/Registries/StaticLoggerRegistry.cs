using System;

namespace MFDLabs.Logging
{
    public static class StaticLoggerRegistry
    {
        public static ILogger Instance
        {
            get
            {
                if (_instance == null) throw new UninitializedLoggerException();
                return _instance;
            }
            private set => _instance = value;
        }
        public static bool HasLogger => _instance != null;

        public static void SetLogger(ILogger logger) 
            => Instance = logger ?? throw new ArgumentNullException(nameof(logger));

        private static ILogger _instance;
    }
}
