﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Backtrace.Model.JsonData
{
    /// <summary>
    /// Collect all source data information about current program
    /// </summary>
    public class SourceCodeData
    {
        /// <summary>
        /// Single instance of source data frame
        /// </summary>
        public class SourceCode
        {
            /// <summary>
            /// Line number in source code where exception occurs
            /// </summary>
            [JsonProperty(PropertyName = "startLine")]
            public int StartLine { get; set; }

            /// <summary>
            /// Column number in source code where exception occurs
            /// </summary>
            [JsonProperty(PropertyName = "startColumn")]
            public int StartColumn { get; set; }

            private string _sourceCodeFullPath { get; set; }
            /// <summary>
            /// Full path to source code where exception occurs
            /// </summary>
            [JsonProperty(PropertyName = "path")]
            public string SourceCodeFullPath
            {
                get
                {
                    if (!string.IsNullOrEmpty(_sourceCodeFullPath))
                    {
                        return Path.GetFileName(_sourceCodeFullPath);
                    }
                    return string.Empty;
                }
                set
                {
                    _sourceCodeFullPath = value;
                }
            }

            /// <summary>
            /// Get a SourceData instance from Exception stack
            /// </summary>
            /// <param name="stackFrame">Exception Stack</param>
            /// <returns>New instance of SoruceCode</returns>
            public static SourceCode FromExceptionStack(BacktraceStackFrame stackFrame)
            {
                return new SourceCode()
                {
                    StartColumn = stackFrame.Column,
                    StartLine = stackFrame.Line,
                    SourceCodeFullPath = stackFrame.SourceCodeFullPath
                };
            }
        }

        /// <summary>
        /// Source code information about current executed program
        /// </summary>
        public Dictionary<string, SourceCode> data = new Dictionary<string, SourceCode>();
        internal SourceCodeData(IEnumerable<BacktraceStackFrame> exceptionStack)
        {
            if (exceptionStack == null || exceptionStack.Count() == 0)
            {
                return;
            }
            foreach (var exception in exceptionStack)
            {
                if (string.IsNullOrEmpty(exception.SourceCode))
                {
                    continue;
                }
                string id = exception.SourceCode;
                var value = SourceCode.FromExceptionStack(exception);
                data.Add(id, value);
            }
        }
    }
}
