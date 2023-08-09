using System;

namespace MFDLabs.Threading.Extensions
{
    public static class ActionExtensions
    {
        public static void InvokeAfter(this Action self, TimeSpan time) => TaskHelper.SetTimeout(self, time);
    }
}
