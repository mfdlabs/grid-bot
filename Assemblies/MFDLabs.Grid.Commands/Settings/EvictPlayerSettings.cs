namespace MFDLabs.Grid.Commands
{
    public class EvictPlayerSettings
    {
        public long PlayerId { get; }

        public EvictPlayerSettings(long playerId)
        {
            PlayerId = playerId;
        }
    }
}