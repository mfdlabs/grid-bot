namespace MFDLabs.Networking.Replication
{

    public interface IReplicationProcessor
    {
        ReplicationProcessorResult OnReceive(ref Packet packet);
    }
}