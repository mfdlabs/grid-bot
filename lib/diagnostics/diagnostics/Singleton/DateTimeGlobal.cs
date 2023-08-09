using System;
using System.Diagnostics;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Diagnostics
{
    [DebuggerStepThrough]
    public static class DateTimeGlobal
    {
        [DebuggerStepThrough]
        public static string GetNowAsIso()
        {
            return DateTime.Now.ToIso();
        }

        [DebuggerStepThrough]
        public static string GetUtcNowAsIso()
        {
            return DateTime.UtcNow.ToIso();
        }

        [DebuggerStepThrough]
        public static string GetFileSafeUtcNowAsIso()
        {
            return DateTime.Now.ToIso().MakeFileSafeString().Replace("-", "");
        }
    }
}
