﻿using Backtrace.Types;

namespace Backtrace.Model.Database
{
    /// <summary>
    /// Backtrace library database settings
    /// </summary>
    public class BacktraceDatabaseSettings
    {
        public BacktraceDatabaseSettings(string path)
        {
            DatabasePath = path;
        }
        /// <summary>
        /// Directory path where reports and minidumps are stored
        /// </summary>
        public string DatabasePath { get; private set; }

        /// <summary>
        /// Maximum number of stored reports in Database. If value is equal to zero, then limit not exists
        /// </summary>
        public uint MaxRecordCount { get; set; } = 0;

        /// <summary>
        /// Database size in MB
        /// </summary>
        private long _maxDatabaseSize = 0;

        /// <summary>
        /// Maximum database size in MB. If value is equal to zero, then size is unlimited
        /// </summary>
        public long MaxDatabaseSize
        {
            get
            {
                //convert megabyte to bytes
                return _maxDatabaseSize * 1000 * 1000;
            }
            set
            {
                _maxDatabaseSize = value;
            }
        }

        /// <summary>
        /// Resend report when http client throw exception
        /// </summary>
        public bool AutoSendMode { get; set; } = false;

        /// <summary>
        /// Retry behaviour
        /// </summary>
        public RetryBehavior RetryBehavior { get; set; } = RetryBehavior.ByInterval;

        /// <summary>
        /// How much seconds library should wait before next retry.
        /// </summary>
        public uint RetryInterval { get; set; } = 5;

        /// <summary>
        /// Maximum number of retries
        /// </summary>
        public uint RetryLimit { get; set; } = 3;

        public DeduplicationStrategy DeduplicationStrategy { get; set; } = DeduplicationStrategy.None;

        public RetryOrder RetryOrder { get; set; } = RetryOrder.Queue;
    }
}
