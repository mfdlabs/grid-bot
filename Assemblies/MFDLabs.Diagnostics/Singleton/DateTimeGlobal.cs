using MFDLabs.Abstractions;
using MFDLabs.Diagnostics.Extensions;
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
            return DateTime.Now.ToIso()
        }

        [DebuggerStepThrough]
        public string GetUtcNowAsISO()
        {
            return DateTime.UtcNow.ToIso();
        }
    }
}
