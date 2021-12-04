﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Backtrace.Extensions
{
    /// <summary>
    /// All extensions available for Thread class
    /// </summary>
    public static class ThreadExtensions
    {
        /// <summary>
        /// Generate a valid thread name for passed thread
        /// </summary>
        /// <returns>Thread name</returns>
        public static string GenerateValidThreadName(this Thread thread)
        {
            //generate temporary thread name
            //thread name cannot be "null" or null or empty string
            //in worst scenario thread name should be managedThreadId 
            var threadName = thread.Name;
            threadName = string.IsNullOrEmpty(threadName)
                        ? thread.ManagedThreadId.ToString()
                        : threadName;

            return threadName;
        }
    }
}
