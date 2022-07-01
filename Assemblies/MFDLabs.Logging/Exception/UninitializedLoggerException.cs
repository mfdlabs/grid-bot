using System;

namespace MFDLabs.Logging
{
    /// <summary>
    /// Thrown when the static logger registry does not have a logger registered.
    /// </summary>
    public class UninitializedLoggerException : Exception
    {
        private const string NoLoggerRegistered = "StaticLoggerRegistry does not have a logger registered. " +
                                                  "Please instantiate an ILogger in your component's startup code with " +
                                                  "IsDefaultLog set to true.";
        
        /// <summary>
        /// Creates a new instance of <see cref="UninitializedLoggerException"/>
        /// </summary>
        public UninitializedLoggerException()
            : base(NoLoggerRegistered)
        {
        }
    }
}
