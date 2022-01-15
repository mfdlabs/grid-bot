using FluentAssertions;
using MFDLabs.Grid.Bot.Utility;
using Xunit;

namespace MFDLabs.GridUtility.Unit.Test
{
    public class AdminUtilityTests
    {
        [Fact]
        public void Should_Return_True_If_OwnerID_Valid()
        {
            global::MFDLabs.Grid.Bot.Properties.Settings.Default["BotOwnerID"] = (ulong)123;
            global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();
            
            const ulong ownerId = 123;

            AdminUtility.UserIsOwner(ownerId).Should().BeTrue();
        }
        
        [Fact]
        public void Should_Return_False_If_OwnerID_Invalid()
        {
            global::MFDLabs.Grid.Bot.Properties.Settings.Default["BotOwnerID"] = (ulong)123;
            global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();
            
            const ulong ownerId = 124;

            AdminUtility.UserIsOwner(ownerId).Should().BeFalse();
        }
    }
}