namespace MFDLabs.Networking.Replication
{
    public enum ReplicationProcessorResult
    {
        /// <summary>
        /// Continue to the next processor in the pipeline
        /// </summary>
        ContinueProcessing = 0,
    
        /// <summary>
        /// Stop processing and do not continue to the next processor in the pipeline
        /// </summary>
        StopProcessing
    }
}