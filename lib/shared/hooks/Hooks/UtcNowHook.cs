using System;
using System.Globalization;

using Logging;

using Grid.Bot.Interfaces;

namespace Grid.Bot.Hooks
{
    internal sealed class UtcNowHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 't', 'T' };

        public void Callback(char key) 
            => Logger.Singleton.Log("The current time is '{0}'", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
    }
}
