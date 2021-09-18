using System;

namespace MFDLabs.EventLog
{
    public class UninitializedLoggerException : Exception
    {
        public UninitializedLoggerException()
            : base("StaticLoggerRegistry does not have a logger registered. Please instantiate an ILogger in your component's startup code with IsDefaultLog set to true.")
        {
        }
    }
}
