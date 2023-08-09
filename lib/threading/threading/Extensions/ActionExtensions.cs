using System;

namespace Threading.Extensions
{
    public static class ActionExtensions
    {
        public static void InvokeAfter(this Action self, TimeSpan time) => TaskHelper.SetTimeout(self, time);
    }
}
