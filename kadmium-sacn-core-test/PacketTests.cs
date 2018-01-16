using System;
using kadmium_sacn_core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kadmium_sacn_core_test
{
    [TestClass]
    public class PacketTests
    {
        [DataTestMethod]
        [DataRow((short)1, "Something", (byte)1, (byte)1)]
        [DataRow((short)12000, "Something Else", (byte)35, (byte)27)]
        public void TestCreation(short universeID, string sourceName, byte sequenceID, byte priority)
        {
            Guid guid = Guid.NewGuid();

            Random random = new Random();
            int length = random.Next(1, 512);
            byte[] data = new byte[length];

            for(int i = 0; i < length; i++)
            {
                data[i] = (byte)random.Next(0, 255);
            }

            SACNPacket packet = new SACNPacket(universeID, sourceName, guid, sequenceID, data, priority);
            Assert.AreEqual(universeID, packet.UniverseID);
            Assert.AreEqual(sourceName, packet.SourceName);
            Assert.AreEqual(sequenceID, packet.SequenceID);
            Assert.AreEqual(priority, packet.RootLayer.FramingLayer.Priority);
            Assert.AreEqual(data, packet.Data);
        }

        [DataTestMethod]
        [DataRow((short)1, "Something", (byte)1, (byte)1)]
        [DataRow((short)12000, "Something Else", (byte)35, (byte)27)]
        public void TestCreationAndParsing(short universeID, string sourceName, byte sequenceID, byte priority)
        {
            Guid guid = Guid.NewGuid();

            Random random = new Random();
            int length = random.Next(1, 512);
            byte[] data = new byte[length];

            for (int i = 0; i < length; i++)
            {
                data[i] = (byte)random.Next(0, 255);
            }

            SACNPacket packet = new SACNPacket(universeID, sourceName, guid, sequenceID, data, priority);
            SACNPacket parsed = SACNPacket.Parse(packet.ToArray());

            Assert.AreEqual(universeID, parsed.UniverseID);
            Assert.AreEqual(sourceName, parsed.SourceName);
            Assert.AreEqual(sequenceID, parsed.SequenceID);
            Assert.AreEqual(priority, parsed.RootLayer.FramingLayer.Priority);
            Assert.AreEqual(data.Length, parsed.Data.Length);
            for(int i = 0; i < data.Length; i++)
            {
                Assert.AreEqual(data[i], parsed.Data[i]);
            }
        }
    }
}
