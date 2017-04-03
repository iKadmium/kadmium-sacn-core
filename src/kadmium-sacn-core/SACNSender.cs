using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace kadmium_sacn_core
{
    public class SACNSender
    {
        public Guid UUID { get; set; }
        private UdpClient Socket { get; set; }
        public IPAddress UnicastAddress { get; set; }
        public bool Multicast { get { return UnicastAddress == null; } }
        public int Port { get; set; }
        public string SourceName { get; set; }

        byte sequenceID = 0;

        public SACNSender(Guid uuid, string sourceName, int port)
        {
            SourceName = sourceName;
            UUID = uuid;
            Socket = new UdpClient();
            Port = port;
        }

        public SACNSender(Guid uuid, string sourceName) : this(uuid, sourceName, SACNCommon.SACN_PORT) { }

        /// <summary>
        /// Multicast send
        /// </summary>
        /// <param name="universeID">The universe ID to multicast to</param>
        /// <param name="data">Up to 512 bytes of DMX data</param>
        public async Task Send(Int16 universeID, byte[] data)
        {
            SACNPacket packet = new SACNPacket(universeID, SourceName, UUID, sequenceID++, data);
            byte[] packetBytes = packet.ToArray();
            SACNPacket parsed = SACNPacket.Parse(packetBytes);
            await Socket.SendAsync(packetBytes, packetBytes.Length, GetEndPoint(universeID, Port));
        }

        /// <summary>
        /// Unicast send
        /// </summary>
        /// <param name="hostname">The hostname to unicast to</param>
        /// <param name="universeID">The Universe ID</param>
        /// <param name="data">Up to 512 bytes of DMX data</param>
        public async Task Send(string hostname, Int16 universeID, byte[] data)
        {
            SACNPacket packet = new SACNPacket(universeID, SourceName, UUID, sequenceID++, data);
            byte[] packetBytes = packet.ToArray();
            SACNPacket parsed = SACNPacket.Parse(packetBytes);
            await Socket.SendAsync(packetBytes, packetBytes.Length, hostname, Port);
        }

        private IPEndPoint GetEndPoint(Int16 universeID, int port)
        {
            if (Multicast)
            {
                return new IPEndPoint(SACNCommon.GetMulticastAddress(universeID), port);
            }
            else
            {
                return new IPEndPoint(UnicastAddress, port);
            }
        }

        public void Close()
        {
            //Socket.Close();
        }
    }
}