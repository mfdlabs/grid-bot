using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class HelloWorld : IStateSpecificCommandHandler
    {
        public string CommandName => "Hello World";

        public string CommandDescription => "Invokes a hello world request to the remote service";

        public string[] CommandAliases => new string[] { "hw" };

        public bool Internal => false;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            await message.ReplyAsync(await SoapUtility.Singleton.HelloWorldAsync());
        }
    }
}
