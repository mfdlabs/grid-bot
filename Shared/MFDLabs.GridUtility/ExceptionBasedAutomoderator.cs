/* Copyright MFDLABS Corporation. All rights reserved. */

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

        private static readonly ConcurrentDictionary<ulong, (DateTime, Atomic)> TrackedUsers = new();

        public static (DateTime, Atomic) GetOrCreateTrack(this IUser user, Atomic count, TimeSpan lease)
        {
            return TrackedUsers.GetOrAdd(user.Id, (DateTime.Now.Add(lease), count));
        }

        public static (DateTime, Atomic) UpdateOrCreateTrack(this IUser user, Atomic? count, TimeSpan? lease, bool incrementLastCount = false)
        {
            (DateTime? expires, Atomic? count) updateTuple;
            if (lease == null) updateTuple = (null, count);
            else updateTuple = (DateTime.Now.Add(lease.Value), count);

            return TrackedUsers.AddOrUpdate(user.Id, 
                _ =>
                {
                    if (updateTuple.count == null) updateTuple.count = 0;
                    return (updateTuple.expires.Value, updateTuple.count.Value);
                },
                (_, old) =>
                {
                    if (updateTuple.count == null && !incrementLastCount) updateTuple.count = old.Item2;
                    if (incrementLastCount)
                    {
                        updateTuple.count = old.Item2++;
                        updateTuple.expires = old.Item1;
                    }
                    return (updateTuple.expires.Value, updateTuple.count.Value);
                }
            );
        }

        public static bool DetermineIfUserHasExceededExceptionLimit(this IUser user)
        {
            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorEnabled) return false;

            var (exceptionCounterExpires, exceptionCounter) = user.GetOrCreateTrack(0, global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorLeaseTimeSpanAddition);

            if (exceptionCounterExpires < DateTime.Now)
            {
                Logger.Singleton.Warning("User {0}'s exception monitor has expired at count {1}", user.Id, exceptionCounter);

                if (exceptionCounter == 0)
                {
                    Logger.Singleton.Info("They had an exception counter of 0, so they were just created, return false");
                    return false;
                }

                // Exlusive in case another thread had hit them just in the nick of time :/
                if (exceptionCounter > global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorMaxExceptionHitsBeforeBlacklist)
                {
                    Logger.Singleton.Warning("Their exception counter exceeded the maximum before blacklist, return true");
                    return true;
                }

                Logger.Singleton.Info("The user didn't exceed the maximum before blacklist, reset their track.");
                user.UpdateOrCreateTrack(0, global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorLeaseTimeSpanAddition);

                return false;
            }

            Logger.Singleton.Info("User's track hasn't expired, check if their exception counter exceeded the maximum before blacklist.");


            return exceptionCounter > global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorMaxExceptionHitsBeforeBlacklist;
        }

        public static bool CheckIfUserShouldBeBlacklisted(this IUser user)
        {
            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorEnabled) return false;

            if (user.DetermineIfUserHasExceededExceptionLimit())
            {
                Logger.Singleton.Warning("The user exceeded their exception limit, blacklist them now.");
                user.Blacklist();
                return true;
            }

            return false;
        }

        public static void IncrementExceptionLimit(this IUser user)
        {
            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExceptionBasedAutomoderatorEnabled) return;

            if (user.CheckIfUserShouldBeBlacklisted()) return;

            Logger.Singleton.Verbose("User {0} has not exceeded the exception limit, increment their count atomically.", user.Id);

            user.UpdateOrCreateTrack(null, null, true);
        }
    }
}
