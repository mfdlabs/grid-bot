using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Discord.RbxUsers.Client.Models;

namespace MFDLabs.Discord.RbxUsers.Client
{
    public interface IRbxDiscordUsersClient
    {
        RobloxUserResponse ResolveRobloxUserById(ulong discordId);
        Task<RobloxUserResponse> ResolveRobloxUserByIdAsync(ulong discordId, CancellationToken cancellationToken);
    }
}
