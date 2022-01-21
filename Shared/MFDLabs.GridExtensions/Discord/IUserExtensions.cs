using System.Threading.Tasks;
using Discord;
using MFDLabs.Analytics.Google;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class IUserExtensions
    {
        public static bool IsAdmin(this IUser user) => AdminUtility.UserIsAdmin(user);
        public static bool IsPrivilaged(this IUser user) => AdminUtility.UserIsPrivilaged(user);
        public static bool IsOwner(this IUser user) => AdminUtility.UserIsOwner(user);
        public static Task<long?> GetRobloxIdAsync(this IUser user) => UserUtility.GetRobloxIdByIUserAsync(user);
        public static long? GetRobloxId(this IUser user) => UserUtility.GetRobloxIdByIUser(user);
        public static void PageViewed(this IUser user, string location)
            => GoogleAnalyticsManager.TrackPageView(user.Id.ToString(), location);
        public static Task PageViewedAsync(this IUser user, string location)
            => GoogleAnalyticsManager.TrackPageViewAsync(user.Id.ToString(), location);
        public static void FireEvent(this IUser user, string @event, string extraContext = "none") 
            => GoogleAnalyticsManager.TrackEvent(user.Id.ToString(), "UserAction", @event, extraContext);
        public static Task FireEventAsync(this IUser user, string @event, string extraContext = "none") 
            => GoogleAnalyticsManager.TrackEventAsync(user.Id.ToString(), "UserAction", @event, extraContext);
    }
}
