/* Copyright MFDLABS Corporation. All rights reserved. */

using System.Threading.Tasks;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnLoggedOut
    {
        public static Task Invoke()
        {
            Logger.Singleton.Debug("BotGlobal logged out.");
            return Task.CompletedTask;
        }
    }
}
