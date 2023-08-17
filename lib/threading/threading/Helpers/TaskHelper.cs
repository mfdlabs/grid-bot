using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Threading
{
    [DebuggerStepThrough]
    [DebuggerDisplay("Task Helper Global")]
    public static class TaskHelper
    {
        public static void SetTimeout(Action action, TimeSpan time)
        {
            Task.Run(async () =>
            {
                await Task.Delay(time);

                action();
            });
        }
        public static void SetTimeoutFromDays(Action action, double days) => SetTimeout(action, TimeSpan.FromDays(days));
        public static void SetTimeoutFromHours(Action action, double hours) => SetTimeout(action, TimeSpan.FromHours(hours));
        public static void SetTimeoutFromMilliseconds(Action action, double milliseconds) => SetTimeout(action, TimeSpan.FromMilliseconds(milliseconds));
        public static void SetTimeoutFromMinutes(Action action, double minutes) => SetTimeout(action, TimeSpan.FromMinutes(minutes));
        public static void SetTimeoutFromSeconds(Action action, double seconds) => SetTimeout(action, TimeSpan.FromSeconds(seconds));
        public static void SetTimeoutFromTicks(Action action, long ticks) => SetTimeout(action, TimeSpan.FromTicks(ticks));
    }
}
