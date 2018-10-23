using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace kadmium_sacn_core
{
    public class SACNPacket
    {
        public static UInt16 FLAGS = (0x7 << 12);
        public static UInt16 FIRST_FOUR_BITS_MASK = 0b1111_0000_0000_0000;
        public static UInt16 LAST_TWELVE_BITS_MASK = 0b0000_1111_1111_1111;

        public static int MAX_PACKET_SIZE = 638;

        public RootLayer RootLayer { get; set; }

        public string SourceName { get { return RootLayer.FramingLayer.SourceName; } set { RootLayer.FramingLayer.SourceName = value; } }
        public Guid UUID { get { return RootLayer.UUID; } set { RootLayer.UUID = value; } }
        public byte SequenceID { get { return RootLayer.FramingLayer.SequenceID; } set { RootLayer.FramingLayer.SequenceID = value; } }
        public byte[] Data { get { return RootLayer.FramingLayer.DMPLayer.Data; } set { RootLayer.FramingLayer.DMPLayer.Data = value; } }
        public UInt16 UniverseID { get { return RootLayer.FramingLayer.UniverseID; } set { RootLayer.FramingLayer.UniverseID = value; } }

        public SACNPacket(UInt16 universeID, String sourceName, Guid uuid, byte sequenceID, byte[] data, byte priority)
        {
            RootLayer = new RootLayer(uuid, sourceName, universeID, sequenceID, data, priority);
        }

        public SACNPacket(RootLayer rootLayer)
        {
            RootLayer = rootLayer;
        }

        public static SACNPacket Parse(byte[] packet)
        {
            using (var stream = new MemoryStream(packet))
            using (var buffer = new BigEndianBinaryReader(stream))
            {
                var rootLayer = RootLayer.Parse(buffer);

                return new SACNPacket(rootLayer);
            }
        }

        public byte[] ToArray()
        {
            return RootLayer.ToArray();
        }
    }

    public class RootLayer
    {
        static readonly Int16 PREAMBLE_LENGTH = 0x0010;
        static readonly Int16 POSTAMBLE_LENGTH = 0x0000;
        static readonly byte[] PACKET_IDENTIFIER = new byte[] {0x41, 0x53, 0x43, 0x2d, 0x45,
                                             0x31, 0x2e, 0x31, 0x37, 0x00,
                                             0x00, 0x00};
        static readonly Int32 ROOT_VECTOR = 0x00000004;

        public FramingLayer FramingLayer { get; set; }
        public Int16 Length { get { return (Int16)(38 + FramingLayer.Length); } }
        public Guid UUID { get; set; }

        public RootLayer(Guid uuid, string sourceName, UInt16 universeID, byte sequenceID, byte[] data, byte priority)
        {
            UUID = uuid;
            FramingLayer = new FramingLayer(sourceName, universeID, sequenceID, data, priority);
        }

        public RootLayer()
        {

        }

        public byte[] ToArray()
        {
            using (var stream = new MemoryStream(Length))
            using (var buffer = new BigEndianBinaryWriter(stream))
            {
                buffer.Write(PREAMBLE_LENGTH);
                buffer.Write(POSTAMBLE_LENGTH);
                buffer.Write(PACKET_IDENTIFIER);
                UInt16 flagsAndRootLength = (UInt16)(SACNPacket.FLAGS | (UInt16)(Length - 16));
                buffer.Write(flagsAndRootLength);
                buffer.Write(ROOT_VECTOR);
                buffer.Write(UUID.ToByteArray());

                buffer.Write(FramingLayer.ToArray());

                return stream.ToArray();
            }
        }

        internal static RootLayer Parse(BigEndianBinaryReader buffer)
        {
            Int16 preambleLength = buffer.ReadInt16();
            Debug.Assert(preambleLength == PREAMBLE_LENGTH);
            Int16 postambleLength = buffer.ReadInt16();
            Debug.Assert(postambleLength == POSTAMBLE_LENGTH);
            byte[] packetIdentifier = buffer.ReadBytes(12);
            Debug.Assert(packetIdentifier.SequenceEqual(PACKET_IDENTIFIER));
            UInt16 flagsAndRootLength = (UInt16)buffer.ReadInt16();
            UInt16 flags = (UInt16)(flagsAndRootLength & SACNPacket.FIRST_FOUR_BITS_MASK);
            Debug.Assert(flags == SACNPacket.FLAGS);
            UInt16 length = (UInt16)(flagsAndRootLength & SACNPacket.LAST_TWELVE_BITS_MASK);
            Int32 vector = buffer.ReadInt32();
            Debug.Assert(vector == ROOT_VECTOR);
            Guid cid = new Guid(buffer.ReadBytes(16));

            RootLayer rootLayer = new RootLayer()
            {
                UUID = cid,
                FramingLayer = FramingLayer.Parse(buffer)
            };
            
            return rootLayer;
        }
    }

    public class FramingLayer
    {
        static readonly Int32 FRAMING_VECTOR = 0x00000002;
        static readonly Int16 RESERVED = 0;
        static int SourceNameLength = 64;

        public DMPLayer DMPLayer { get; set; }
        public UInt16 Length { get { return (UInt16)(13 + SourceNameLength + DMPLayer.Length); } }
        public string SourceName { get; set; }
        public UInt16 UniverseID { get; set; }
        public byte SequenceID { get; set; }
        public byte Priority { get; set; }
        public FramingOptions Options { get; set; }

        public FramingLayer(string sourceName, UInt16 universeID, byte sequenceID, byte[] data, byte priority)
        {
            SourceName = sourceName;
            UniverseID = universeID;
            SequenceID = sequenceID;
            Options = new FramingOptions();
            DMPLayer = new DMPLayer(data);
            Priority = priority;
            Options = new FramingOptions();
        }

        public FramingLayer()
        {
        }

        public byte[] ToArray()
        {
            byte[] array;
            using (var stream = new MemoryStream(Length))
            using (var buffer = new BigEndianBinaryWriter(stream))
            {

                UInt16 flagsAndFramingLength = (UInt16)(SACNPacket.FLAGS | Length);
                buffer.Write(flagsAndFramingLength);
                buffer.Write(FRAMING_VECTOR);
                buffer.Write(Encoding.UTF8.GetBytes(SourceName));
                buffer.Write(Enumerable.Repeat((byte)0, 64 - SourceName.Length).ToArray());
                buffer.Write(Priority);
                buffer.Write(RESERVED);
                buffer.Write(SequenceID);
                buffer.Write(Options.ToByte());
                buffer.Write(UniverseID);

                buffer.Write(DMPLayer.ToArray());

                array = stream.ToArray();
            }
            return array;
        }

        internal static FramingLayer Parse(BigEndianBinaryReader buffer)
        {
            UInt16 flagsAndFramingLength = (UInt16)buffer.ReadInt16();
            UInt16 flags = (UInt16)(flagsAndFramingLength & SACNPacket.FIRST_FOUR_BITS_MASK);
            Debug.Assert(flags == SACNPacket.FLAGS);
            UInt16 length = (UInt16)(flagsAndFramingLength & SACNPacket.LAST_TWELVE_BITS_MASK);

            Int32 vector2 = buffer.ReadInt32();
            Debug.Assert(vector2 == FRAMING_VECTOR);
            byte[] sourceNameBytes = buffer.ReadBytes(64);
            string sourceName = new string(Encoding.UTF8.GetChars(sourceNameBytes)).TrimEnd('\0');
            byte priority = buffer.ReadByte();
            Int16 reserved = buffer.ReadInt16();
            Debug.Assert(reserved == RESERVED);
            byte sequenceID = buffer.ReadByte();
            byte optionsByte = buffer.ReadByte();
            FramingOptions options = FramingOptions.Parse(optionsByte);

            UInt16 universeID = buffer.ReadUInt16();

            var framingLayer = new FramingLayer
            {
                SequenceID = sequenceID,
                SourceName = sourceName,
                DMPLayer = DMPLayer.Parse(buffer),
                Options = options,
                UniverseID = universeID,
                Priority = priority
            };

            return framingLayer;
        }
    }

    public class FramingOptions
    {
        public bool PreviewData { get; set; }
        public bool StreamTerminated { get; set; }
        public bool ForceSynchronization { get; set; }

        private static readonly byte FORCE_SYNCHRONIZATION = 0b0000_1000;
        private static readonly byte STREAM_TERMINATED = 0b0000_0100;
        private static readonly byte PREVIEW_DATA = 0b0000_0010;
        
        public FramingOptions()
        {
            
        }

        public static FramingOptions Parse(byte optionsByte)
        {
            FramingOptions options = new FramingOptions();
            if((optionsByte & FORCE_SYNCHRONIZATION) != 0)
            {
                options.ForceSynchronization = true;
            }
            if ((optionsByte & STREAM_TERMINATED) != 0)
            {
                options.StreamTerminated = true;
            }
            if ((optionsByte & PREVIEW_DATA) != 0)
            {
                options.PreviewData = true;
            }
            return options;
        }

        public byte ToByte()
        {
            byte returnVal = 0;
            if(PreviewData)
            {
                returnVal = (byte)(returnVal | PREVIEW_DATA);
            }
            if (StreamTerminated)
            {
                returnVal = (byte)(returnVal | STREAM_TERMINATED);
            }
            if (ForceSynchronization)
            {
                returnVal = (byte)(returnVal | FORCE_SYNCHRONIZATION);
            }
            return returnVal;
        }
    }

    public class DMPLayer
    {
        static readonly byte DMP_VECTOR = 2;
        static readonly byte ADDRESS_TYPE_AND_DATA_TYPE = 0xa1;
        static readonly Int16 FIRST_PROPERTY_ADDRESS = 0;
        static readonly Int16 ADDRESS_INCREMENT = 1;
        static readonly byte ZERO_ADDRESS = 0x00;

        public byte StartCode { get; set; }
        public Int16 Length { get { return (Int16)(11 + Data.Length); } }

        public byte[] Data { get; set; }

        public DMPLayer(byte[] data)
        {
            Data = data;
        }

        public byte[] ToArray()
        {
            byte[] array;
            using (var stream = new MemoryStream(Length))
            using (var buffer = new BigEndianBinaryWriter(stream))
            {
                UInt16 flagsAndDMPLength = (UInt16)(SACNPacket.FLAGS | (UInt16)Length);

                buffer.Write(flagsAndDMPLength);
                buffer.Write(DMP_VECTOR);
                buffer.Write(ADDRESS_TYPE_AND_DATA_TYPE);
                buffer.Write(FIRST_PROPERTY_ADDRESS);
                buffer.Write(ADDRESS_INCREMENT);
                buffer.Write((Int16)(Data.Length + 1));
                buffer.Write(StartCode);
                buffer.Write(Data);

                array = stream.ToArray();
            }
            return array;
        }

        internal static DMPLayer Parse(BigEndianBinaryReader buffer)
        {
            Int16 flagsAndDMPLength = buffer.ReadInt16();
            byte vector3 = buffer.ReadByte();
            Debug.Assert(vector3 == DMP_VECTOR);
            byte addressTypeAndDataType = buffer.ReadByte();
            Debug.Assert(addressTypeAndDataType == ADDRESS_TYPE_AND_DATA_TYPE);
            Int16 firstPropertyAddress = buffer.ReadInt16();
            Debug.Assert(firstPropertyAddress == FIRST_PROPERTY_ADDRESS);
            Int16 addressIncrement = buffer.ReadInt16();
            Debug.Assert(addressIncrement == ADDRESS_INCREMENT);
            Int16 propertyValueCount = buffer.ReadInt16();

            byte startCode = buffer.ReadByte();
            byte[] properties = buffer.ReadBytes(propertyValueCount - 1);

            var dmpLayer = new DMPLayer(properties)
            {
                StartCode = startCode
            };
            return dmpLayer;
        }
    }
}
