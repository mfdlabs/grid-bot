namespace Grid.Bot.Utility;

using System;
using System.Linq;
using System.Collections.Concurrent;

using Discord;

using Logging;

using Threading;

/// <summary>
/// Simple class for watch dogging users exception counts.
/// </summary>
public static class ExceptionBasedAutomoderator
{
    private static void Blacklist(this IUser user)
    {
        if (!AdminUtility.UserIsBlacklisted(user))
        {
            var blIds = DiscordRolesSettings.Singleton.BlacklistedUserIds.ToList();
            blIds.Add(user.Id);

            DiscordRolesSettings.Singleton.BlacklistedUserIds = blIds.ToArray();
        }
    }

    private static readonly ConcurrentDictionary<ulong, (DateTime, Atomic<int>)> _trackedUsers = new();

    /// <summary>
    /// Get or create a tracked <see cref="IUser"/>
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <param name="count">The count.</param>
    /// <param name="lease">The lease.</param>
    /// <returns>The expiration and count.</returns>
    public static (DateTime, Atomic<int>) GetOrCreateTrack(this IUser user, Atomic<int> count, TimeSpan lease)
        => _trackedUsers.GetOrAdd(user.Id, (DateTime.Now.Add(lease), count));

    /// <summary>
    /// Update or create a tracked <see cref="IUser"/>
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <param name="count">The count.</param>
    /// <param name="lease">The lease.</param>
    /// <param name="incrementLastCount">Should increment count.</param>
    /// <returns>The expiration and count.</returns>
    public static (DateTime, Atomic<int>) UpdateOrCreateTrack(this IUser user, Atomic<int>? count, TimeSpan? lease, bool incrementLastCount = false)
    {
        (DateTime? expires, Atomic<int>? count) updateTuple;
        if (lease == null)
            updateTuple = (null, count);
        else 
            updateTuple = (DateTime.Now.Add(lease.Value), count);

        return _trackedUsers.AddOrUpdate(user.Id,
            _ =>
            {
                if (updateTuple.count == null) 
                    updateTuple.count = 0;

                return (updateTuple.expires.Value, updateTuple.count.Value);
            },
            (_, old) =>
            {
                if (updateTuple.count == null && !incrementLastCount)
                    updateTuple.count = old.Item2;

                if (incrementLastCount)
                {
                    updateTuple.count = old.Item2++;
                    updateTuple.expires = old.Item1;
                }

                return (updateTuple.expires.Value, updateTuple.count.Value);
            }
        );
    }

    /// <summary>
    /// Determines if the <see cref="IUser"/> has exceeded the max allotted exception counts.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public static bool DetermineIfUserHasExceededExceptionLimit(this IUser user)
    {
        if (!ExceptionBasedAutomoderatorSettings.Singleton.ExceptionBasedAutomoderatorEnabled) return false;

        var (exceptionCounterExpires, exceptionCounter) = user.GetOrCreateTrack(
            0,
            ExceptionBasedAutomoderatorSettings.Singleton.ExceptionBasedAutomoderatorLeaseTimeSpanAddition
        );

        if (exceptionCounterExpires < DateTime.Now)
        {
            Logger.Singleton.Warning("User {0}'s exception monitor has expired at count {1}", user.Id, exceptionCounter);

            if (exceptionCounter == 0)
            {
                Logger.Singleton.Information("They had an exception counter of 0, so they were just created, return false");
                return false;
            }

            // Exlusive in case another thread had hit them just in the nick of time :/
            if (exceptionCounter > ExceptionBasedAutomoderatorSettings.Singleton.ExceptionBasedAutomoderatorMaxExceptionHitsBeforeBlacklist)
            {
                Logger.Singleton.Warning("Their exception counter exceeded the maximum before blacklist, return true");

                return true;
            }

            Logger.Singleton.Information("The user didn't exceed the maximum before blacklist, reset their track.");
            user.UpdateOrCreateTrack(0, ExceptionBasedAutomoderatorSettings.Singleton.ExceptionBasedAutomoderatorLeaseTimeSpanAddition);

            return false;
        }

        Logger.Singleton.Information("User's track hasn't expired, check if their exception counter exceeded the maximum before blacklist.");


        return exceptionCounter > ExceptionBasedAutomoderatorSettings.Singleton.ExceptionBasedAutomoderatorMaxExceptionHitsBeforeBlacklist;
    }

    /// <summary>
    /// Checks if the <see cref="IUser"/> should be blacklisted.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    /// <returns>True if the user should be blacklisted.</returns>
    public static bool CheckIfUserShouldBeBlacklisted(this IUser user)
    {
        if (!ExceptionBasedAutomoderatorSettings.Singleton.ExceptionBasedAutomoderatorEnabled) return false;

        if (user.DetermineIfUserHasExceededExceptionLimit())
        {
            Logger.Singleton.Warning("The user exceeded their exception limit, blacklist them now.");
            user.Blacklist();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Increment the exception count for the user.
    /// </summary>
    /// <param name="user">The <see cref="IUser"/></param>
    public static void IncrementExceptionLimit(this IUser user)
    {
        if (!ExceptionBasedAutomoderatorSettings.Singleton.ExceptionBasedAutomoderatorEnabled) return;

        if (user.CheckIfUserShouldBeBlacklisted()) return;

        Logger.Singleton.Debug("User {0} has not exceeded the exception limit, increment their count atomically.", user.Id);

        user.UpdateOrCreateTrack(null, null, true);
    }
}
