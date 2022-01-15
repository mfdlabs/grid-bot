﻿using MFDLabs.Backtrace.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MFDLabs.Backtrace.Model
{
    /// <summary>
    /// Backtrace stack trace
    /// </summary>
    public class BacktraceStackTrace
    {
        /// <summary>
        /// Stack trace frames
        /// </summary>
        public List<BacktraceStackFrame> StackFrames { get; set; } = new List<BacktraceStackFrame>();
        /// <summary>
        /// Calling assembly
        /// </summary>
        internal Assembly CallingAssembly { get; set; }

        /// <summary>
        /// Current exception
        /// </summary>
        private readonly Exception _exception;
        private readonly bool _reflectionMethodName;
        public BacktraceStackTrace(Exception exception, bool reflectionMethodName)
        {
            _exception = exception;
            _reflectionMethodName = reflectionMethodName;
            Initialize();
        }

        private void Initialize()
        {
            bool generateExceptionInformation = _exception != null;
            var stackTrace = new StackTrace(true);
            if (_exception != null)
            {
                if (CallingAssembly == null)
                {
                    CallingAssembly = _exception.TargetSite?.DeclaringType?.Assembly;
                }
                var exceptionStackTrace = new StackTrace(_exception, true);
                var exceptionFrames = exceptionStackTrace.GetFrames();
                SetStacktraceInformation(exceptionFrames, true);
            }
            else
            {
                //reverse frame order
                var frames = stackTrace.GetFrames();
                SetStacktraceInformation(frames, generateExceptionInformation);
            }
            //Library didn't found Calling assembly
            //The reason for this behaviour is because we throw exception from TaskScheduler
            //or other method that don't generate valid stack trace
            if (CallingAssembly == null)
            {
                CallingAssembly = Assembly.GetExecutingAssembly();
            }
        }

        private void SetStacktraceInformation(StackFrame[] frames, bool generatedByException = false)
        {
            if (frames == null)
            {
                return;
            }
            int startingIndex = 0;
            //get calling assembly information
            bool needCallingAssembly = CallingAssembly == null;
            //determine stack frames generated by Backtrace library
            //if we get stack frame from Backtrace we ignore them and reset stack frame stack
            var executedAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            foreach (var frame in frames)
            {
                var backtraceFrame = new BacktraceStackFrame(frame, generatedByException, _reflectionMethodName);
                //ignore frames generated by Backtrace library
                string assemblyName = backtraceFrame?.Library ?? string.Empty;
                if (assemblyName.Equals(executedAssemblyName) || string.IsNullOrEmpty(assemblyName))
                {
                    continue;
                }
                if (needCallingAssembly && !SystemHelper.SystemAssembly(assemblyName))
                {
                    //we already found calling assembly
                    needCallingAssembly = false;
                    CallingAssembly = backtraceFrame.FrameAssembly;
                }
                if (needCallingAssembly == true)
                {
                    continue;
                }
                StackFrames.Insert(startingIndex, backtraceFrame);
                startingIndex++;
            }
        }
    }
}
