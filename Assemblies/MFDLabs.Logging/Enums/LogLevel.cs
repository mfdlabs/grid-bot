namespace MFDLabs.Logging
{
    /// <summary>
    /// Represents the logging level.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// No-Op
        /// </summary>
        None,

        /// <summary>
        /// LC-Event
        /// </summary>
        LifecycleEvent,

        /// <summary>
        /// Error messagw
        /// </summary>
        Error,

        /// <summary>
        /// Warn
        /// </summary>
        Warning,

        /// <summary>
        /// Info
        /// </summary>
        Information,

        /// <summary>
        /// Verbose
        /// </summary>
        Verbose
    }
}
