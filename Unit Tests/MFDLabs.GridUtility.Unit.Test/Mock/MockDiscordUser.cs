using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace MFDLabs.GridUtility.Unit.Test.Mock
{
    internal class MockDiscordUser : IUser
    {
        public MockDiscordUser(ulong id)
        {
            Id = id;
        }

        public ulong Id { get; }
        public DateTimeOffset CreatedAt { get; internal set; }
        public string Mention { get; internal set; }
        public UserStatus Status { get; internal set; }
        public IReadOnlyCollection<ClientType> ActiveClients { get; internal set; }
        public IReadOnlyCollection<IActivity> Activities { get; internal set; }
        public string AvatarId { get; internal set; }
        public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
        {
            throw new NotImplementedException();
        }

        public string GetDefaultAvatarUrl()
        {
            throw new NotImplementedException();
        }

        public string Discriminator { get; internal set; }
        public ushort DiscriminatorValue { get; internal set; }
        public bool IsBot { get; internal set; }
        public bool IsWebhook { get; internal set; }
        public string Username { get; internal set; }
        public UserProperties? PublicFlags { get; internal set; }
        public Task<IDMChannel> CreateDMChannelAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}