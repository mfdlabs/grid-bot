using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class OpenGridServerHook : IConsoleHook
    {
        public char[] HookKeys => new char[] { 'o', 'O' };

        public void Callback(char key)
        {
            SystemUtility.Singleton.OpenGridServer();
        }
    }
}
