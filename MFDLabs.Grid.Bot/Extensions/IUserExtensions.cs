using System.Threading.Tasks;
using Discord;
using MFDLabs.Analytics.Google;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Extensions
{
    internal static class IUserExtensions
    {
        public static bool IsAdmin(this IUser user) => AdminUtility.Singleton.UserIsAdmin(user);
        public static bool IsPrivilaged(this IUser user) => AdminUtility.Singleton.UserIsPrivilaged(user);
        public static bool IsOwner(this IUser user) => AdminUtility.Singleton.UserIsOwner(user);
        public static Task<long?> GetRobloxIDAsync(this IUser user) => UserUtility.Singleton.GetRobloxIDByIUserAsync(user);
        public static long? GetRobloxID(this IUser user) => UserUtility.Singleton.GetRobloxIDByIUser(user);
        public static void FireEvent(this IUser user, string @event, string extraContext = "none") => GoogleAnalyticsManager.Singleton.TrackEvent(user.Id.ToString(), "UserAction", @event, extraContext, 1);
        public static Task FireEventAsync(this IUser user, string @event, string extraContext = "none") => GoogleAnalyticsManager.Singleton.TrackEventAsync(user.Id.ToString(), "UserAction", @event, extraContext, 1);
    }
}
