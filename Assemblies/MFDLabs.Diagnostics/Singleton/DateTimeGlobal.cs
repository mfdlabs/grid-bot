using MFDLabs.Abstractions;
using System;
using System.Diagnostics;

namespace MFDLabs.Diagnostics
{
    [DebuggerStepThrough]
    public sealed class DateTimeGlobal : SingletonBase<DateTimeGlobal>
    {
        [DebuggerStepThrough]
        public string GetNowAsISO()
        {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffZ");
        }

        [DebuggerStepThrough]
        public string GetUtcNowAsISO()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffZ");
        }
    }
}
