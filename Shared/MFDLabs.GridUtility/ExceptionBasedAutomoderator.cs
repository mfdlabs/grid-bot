using System;
using System.Collections.Concurrent;
using System.Linq;
using Discord;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;
using MFDLabs.Threading;

namespace MFDLabs.Grid.Bot.Utility
{


    public static class ExceptionBasedAutomoderator
    {
        private static void Blacklist(this IUser user)
        {
            var blacklistedUsers = global::MFDLabs.Grid.Bot.Properties.Settings.Default.BlacklistedDiscordUserIds;

            if (!blacklistedUsers.Contains(user.Id.ToString()))
            {
                var blIds = blacklistedUsers.Split(',').ToList();
                blIds.Add(user.Id.ToString());
                global::MFDLabs.Grid.Bot.Properties.Settings.Default["BlacklistedDiscordUserIds"] = blIds.Join(',');
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();
            }
        }

        private static ConcurrentDictionary<IUser, (DateTime, Atomic)> TrackedUsers = new();

        public static (DateTime, Atomic) GetOrCreateTrack(this IUser user)
        {
            return TrackedUsers.GetOrAdd(user, (DateTime.MinValue, 0));
        }

        public static bool DetermineIfUserHasExceededExceptionLimit(this IUser user)
        {
            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorEnabled) return false;

            var (exceptionCounterExpires, exceptionCounter) = user.GetOrCreateTrack();

            if (exceptionCounterExpires < DateTime.Now)
            {
                SystemLogger.Singleton.Warning("User {0}'s exception monitor has expired at count {1}", user.Id, exceptionCounter);

                if (exceptionCounter == 0)
                {
                    SystemLogger.Singleton.Info("They had an exception counter of 0, so they were just created, return false");
                    return false;
                }

                // Exlusive in case another thread had hit them just in the nick of time :/
                if (exceptionCounter < global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorMaxExceptionHitsBeforeBlacklist)
                {
                    SystemLogger.Singleton.Warning("Their exception counter exceeded the maximum before blacklist, return true");
                    return true;
                }

                SystemLogger.Singleton.Info("The user didn't exceed the maximum before blacklist, reset their track.");
                TrackedUsers.AddOrUpdate(user, _ => (DateTime.MinValue, 0), (_, _) => (DateTime.Now.Add(global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorLeaseTimeSpanAddition), 0));

                return false;
            }


            return exceptionCounter < global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorMaxExceptionHitsBeforeBlacklist;
        }

        public static bool CheckIfUserShouldBeBlacklisted(this IUser user)
        {
            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorEnabled) return false;

            if (user.DetermineIfUserHasExceededExceptionLimit())
            {
                SystemLogger.Singleton.Warning("The user exceeded their exception limit, blacklist them now.");
                user.Blacklist();
                return true;
            }

            return false;
        }

        public static void IncrementExceptionLimit(this IUser user)
        {
            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorEnabled) return;

            if (user.CheckIfUserShouldBeBlacklisted()) return;

            TrackedUsers.AddOrUpdate(
                user,
                _ => (DateTime.Now.Add(global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorLeaseTimeSpanAddition), 0),
                (k, v) =>
                {
                    var (ex, counter) = v;

                    counter++;

                    return (ex, counter);
                }
            );
        }
    }
}
