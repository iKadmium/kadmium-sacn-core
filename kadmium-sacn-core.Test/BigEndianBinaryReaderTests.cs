using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace kadmium_sacn_core.Test
{
    public class BigEndianBinaryReaderTests
    {
        [Theory]
        [InlineData(32768, 0x80, 0x00)]
        public void ReadUInt16(UInt16 expected, byte first, byte second)
        {
            MemoryStream stream = new MemoryStream(new byte[] { first, second });
            BigEndianBinaryReader reader = new BigEndianBinaryReader(stream);
            UInt16 actual = reader.ReadUInt16();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(16384, 0x40, 0x00)]
        public void ReadInt16_Positive(Int16 expected, byte first, byte second)
        {
            MemoryStream stream = new MemoryStream(new byte[] { first, second });
            BigEndianBinaryReader reader = new BigEndianBinaryReader(stream);
            Int16 actual = reader.ReadInt16();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(-8192, 0xE0, 0x00)]
        public void ReadInt16_Negative(Int16 expected, byte first, byte second)
        {
            MemoryStream stream = new MemoryStream(new byte[] { first, second });
            BigEndianBinaryReader reader = new BigEndianBinaryReader(stream);
            Int16 actual = reader.ReadInt16();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(268435456, 0x10, 0x00, 0x00, 0x00)]
        public void ReadInt32_Positive(Int32 expected, byte first, byte second, byte third, byte fourth)
        {
            MemoryStream stream = new MemoryStream(new byte[] { first, second, third, fourth });
            BigEndianBinaryReader reader = new BigEndianBinaryReader(stream);
            Int32 actual = reader.ReadInt32();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(-268435456, 0xF0, 0x00, 0x00, 0x00)]
        public void ReadInt32_Negative(Int32 expected, byte first, byte second, byte third, byte fourth)
        {
            MemoryStream stream = new MemoryStream(new byte[] { first, second, third, fourth });
            BigEndianBinaryReader reader = new BigEndianBinaryReader(stream);
            Int32 actual = reader.ReadInt32();

            Assert.Equal(expected, actual);
        }
    }
}
