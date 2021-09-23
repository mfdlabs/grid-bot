using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Discord.RbxUsers.Client.Models;

namespace MFDLabs.Discord.RbxUsers.Client
{
    public interface IRbxDiscordUsersClient
    {
        RobloxUserResponse ResolveRobloxUserByID(ulong discordID);
        Task<RobloxUserResponse> ResolveRobloxUserByIDAsync(ulong discordID, CancellationToken cancellationToken);
    }
}
