using System;
using System.Collections.Generic;
using System.Net;

namespace MFDLabs.Networking.Replication
{
    /// <summary>
    /// data[0] = was acknowledgement (responding to a send())
    /// data[1++] = data
    /// </summary>
    public interface IPacket : IDisposable
    {
        IPEndPoint MachineAddress { get; }
        
        uint Length { get; }
        
        uint BitLength { get; }
        
        byte[] Data { get; }
        
        /// <summary>
        /// Acknowledgement response to a packet,
        /// this is here so that the receiver will ignore it
        /// and allow the resprective processor use it
        /// </summary>
        bool WasAnAck { get; }
    }
    
    public class Packet : IPacket
    {
        internal Packet(IPEndPoint machineAddress, byte[] rawBuffer)
        {
            
        }

        private void ProcessPacketHeader(IReadOnlyList<byte> rawBuffer)
        {
            WasAnAck = Convert.ToBoolean(rawBuffer[0]);
            Length = (uint)rawBuffer.Count;
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public IPEndPoint MachineAddress { get; private set; }
        public uint Length { get; private set; }
        public uint BitLength { get; private set; }
        public byte[] Data { get; private set; }
        public bool WasAnAck { get; private set; }
    }
}