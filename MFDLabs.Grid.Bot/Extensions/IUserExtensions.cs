using System.Threading.Tasks;
using Discord;
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
    }
}
