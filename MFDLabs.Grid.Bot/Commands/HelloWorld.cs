using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class HelloWorld : IStateSpecificCommandHandler
    {
        public string CommandName => "Grid Server Hello World";
        public string CommandDescription => "Trys to invoke a HelloWorld SOAP request to a Grid Server instance via SoapUtility.";
        public string[] CommandAliases => new string[] { "hw", "helloworld" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            await message.ReplyAsync(await SoapUtility.Singleton.HelloWorldAsync());
        }
    }
}
