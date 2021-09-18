using Discord;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Extensions
{
    internal static class IUserExtensions
    {
        public static bool IsAdmin(this IUser user)
        {
            return AdminUtility.Singleton.UserIsAdmin(user);
        }

        public static bool IsPrivilaged(this IUser user)
        {
            return AdminUtility.Singleton.UserIsPrivilaged(user);
        }
    }
}
