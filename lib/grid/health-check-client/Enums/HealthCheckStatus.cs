namespace MFDLabs.Grid
{
    /// <summary>
    /// Status returned from a health check.
    /// </summary>
    public enum HealthCheckStatus
    {
        /// <summary>
        /// The health check was successful.
        /// </summary>
        Success,

        /// <summary>
        /// The health check failed.
        /// </summary>
        Failure,

        /// <summary>
        /// The health check timed out.
        /// </summary>
        Timeout,

        /// <summary>
        /// The health check was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// An unknown error occurred.
        /// </summary>
        UnknownError
    }
}
