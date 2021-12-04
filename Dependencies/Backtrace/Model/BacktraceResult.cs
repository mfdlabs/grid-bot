﻿using Backtrace.Types;
using Newtonsoft.Json;
using System;

namespace Backtrace.Model
{
    /// <summary>
    /// Send method result
    /// </summary>
    public class BacktraceResult
    {
        /// <summary>
        /// Current report
        /// </summary>
        public BacktraceReport BacktraceReport { get; set; }

        /// <summary>
        /// Inner exception Backtrace status
        /// </summary>
        public BacktraceResult InnerExceptionResult { get; set; } = null;

        private string _message;
        /// <summary>
        /// Message
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
            }
        }

        /// <summary>
        /// Result
        /// </summary>
        public BacktraceResultStatus Status { get; set; } = BacktraceResultStatus.Ok;

        private string _object;
        /// <summary>
        /// Created object id
        /// </summary>
        [JsonProperty(PropertyName = "object")]
        public string Object
        {
            get
            {
                return _object;
            }
            set
            {
                _object = value;
                Status = BacktraceResultStatus.Ok;
            }
        }
        /// <summary>
        /// Backtrace APi can return _rxid instead of ObjectId. 
        /// Use this setter to set _object field correctly for both answers
        /// </summary>
        [JsonProperty(PropertyName = "_rxid")]
        public string RxId
        {
            set
            {
                _object = value;
                Status = BacktraceResultStatus.Ok;
            }
        }

        [JsonIgnore]
        internal Exception Exception;

        /// <summary>
        /// Return exception if BacktraceResult was created by exception information
        /// </summary>
        /// <returns>BacktraceResult exception</returns>
        public Exception GetException()
        {
            return Exception;
        }


        /// <summary>
        /// Set result when client rate limit reached
        /// </summary>
        /// <param name="report">Executed report</param>
        /// <returns>BacktraceResult with limit reached information</returns>
        internal static BacktraceResult OnLimitReached(BacktraceReport report)
        {
            return new BacktraceResult()
            {
                BacktraceReport = report,
                Status = BacktraceResultStatus.LimitReached,
                Message = "Client report limit reached"
            };
        }

        /// <summary>
        /// Set result when error occurs while sending data to API
        /// </summary>
        /// <param name="report">Executed report</param>
        /// <param name="exception">Exception</param>
        /// <returns>BacktraceResult with exception information</returns>
        internal static BacktraceResult OnError(BacktraceReport report, Exception exception)
        {
            var result = new BacktraceResult()
            {
                BacktraceReport = report,
                Message = exception.Message,
                Status = BacktraceResultStatus.ServerError
            };
            result.Exception = exception;
            return result;
        }

        internal void AddInnerResult(BacktraceResult innerResult)
        {
            if (InnerExceptionResult == null)
            {
                InnerExceptionResult = innerResult;
                return;
            }
            InnerExceptionResult.AddInnerResult(innerResult);
        }
    }
}