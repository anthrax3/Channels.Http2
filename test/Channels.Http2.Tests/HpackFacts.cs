﻿using System;
using System.Threading.Tasks;
using Xunit;

namespace Channels.Http2.Tests
{
    public class HpackFacts
    {
        [Theory]
        // C.1.1
        [InlineData("0A", 5, 10)]
        [InlineData("2A", 5, 10)]
        [InlineData("4A", 5, 10)]
        [InlineData("6A", 5, 10)]
        [InlineData("8A", 5, 10)]
        [InlineData("AA", 5, 10)]
        [InlineData("CA", 5, 10)]
        [InlineData("EA", 5, 10)]
        // C.1.2
        [InlineData("1F9A0A", 5, 1337)]
        [InlineData("3F9A0A", 5, 1337)]
        [InlineData("5F9A0A", 5, 1337)]
        [InlineData("7F9A0A", 5, 1337)]
        [InlineData("9F9A0A", 5, 1337)]
        [InlineData("BF9A0A", 5, 1337)]
        [InlineData("DF9A0A", 5, 1337)]
        [InlineData("FF9A0A", 5, 1337)]
        // C.1.3
        [InlineData("2A", 8, 42)]
        public async Task C_1_123(string hex, int n, int expected)
        {
            using (var channelFactory = new ChannelFactory())
            {
                var channel = channelFactory.CreateChannel();
                var buffer = channel.Alloc();

                buffer.Write(HexToBytes(hex));

                var readable = buffer.AsReadableBuffer();
                Assert.Equal((ulong)expected, Hpack.ReadUInt64(ref readable, n));

                await buffer.FlushAsync();
            }
        }

        [Theory]
        [InlineData("0a 6375 7374 6f6d 2d6b 6579", "custom-key")]
        [InlineData("0d 6375 7374 6f6d 2d68 6561 6465 72", "custom-header")]
        [InlineData("8c f1e3 c2e5 f23a 6ba0 ab90 f4ff", "www.example.com")]
        public async Task BasicStringParse(string hex, string expected)
        {
            using (var channelFactory = new ChannelFactory())
            {
                var channel = channelFactory.CreateChannel();
                var buffer = channel.Alloc();

                buffer.Write(HexToBytes(hex));

                var readable = buffer.AsReadableBuffer();
                Assert.Equal(expected, Hpack.ReadString(ref readable));

                await buffer.FlushAsync();
            }
        }

        private static byte[] HexToBytes(string hex)
        {
            if (hex == null) return null;
            hex = hex.Replace(" ", "").Trim();
            byte[] data = new byte[hex.Length / 2];
            for(int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return data;
        }
    }
}