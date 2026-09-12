using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Network
{
    public class PacketFraming
    {
        // 0x03 is a variable-length packet: one byte of ID, two of length.
        private const byte VARIABLE_LENGTH_ID = 0x03;

        [Fact]
        public void A_Header_That_Is_Still_Arriving_Should_Read_As_Incomplete()
        {
            var buffer = new CircularBuffer();
            buffer.Enqueue(new byte[] { VARIABLE_LENGTH_ID, 0x00 });

            PacketHandlers.GetPacketInfo(buffer, buffer.Length, out _, out _, out _)
                .Should()
                .Be(PacketHandlers.PacketReadStatus.Incomplete);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void A_Length_Smaller_Than_Its_Own_Header_Should_Read_As_Malformed(byte declaredLength)
        {
            // The three-byte header cannot be followed by a packet claiming to be
            // shorter than three bytes. Zero is the case that used to hang: the
            // parser consumed nothing, the buffer never drained, and the loop went
            // round again on the same bytes while holding the stream lock.
            var buffer = new CircularBuffer();
            buffer.Enqueue(new byte[] { VARIABLE_LENGTH_ID, 0x00, declaredLength });

            PacketHandlers.GetPacketInfo(buffer, buffer.Length, out _, out int offset, out int length)
                .Should()
                .Be(PacketHandlers.PacketReadStatus.Malformed);

            offset.Should().Be(3);
            length.Should().Be(declaredLength);
        }

        [Fact]
        public void A_Length_That_Covers_Its_Header_Should_Read_As_Complete()
        {
            var buffer = new CircularBuffer();
            buffer.Enqueue(new byte[] { VARIABLE_LENGTH_ID, 0x00, 0x05 });

            PacketHandlers.GetPacketInfo(buffer, buffer.Length, out byte id, out int offset, out int length)
                .Should()
                .Be(PacketHandlers.PacketReadStatus.Complete);

            id.Should().Be(VARIABLE_LENGTH_ID);
            offset.Should().Be(3);
            length.Should().Be(5);
        }

        [Fact]
        public void A_Fixed_Length_Packet_Should_Read_As_Complete_From_Its_Id_Alone()
        {
            // 0x29 is two bytes with a one byte header, so the table answers
            // without needing anything past the id.
            var buffer = new CircularBuffer();
            buffer.Enqueue(new byte[] { 0x29 });

            PacketHandlers.GetPacketInfo(buffer, buffer.Length, out byte id, out int offset, out int length)
                .Should()
                .Be(PacketHandlers.PacketReadStatus.Complete);

            id.Should().Be(0x29);
            offset.Should().Be(1);
            length.Should().BeGreaterOrEqualTo(offset);
        }

        [Fact]
        public void An_Empty_Buffer_Should_Read_As_Incomplete()
        {
            var buffer = new CircularBuffer();

            PacketHandlers.GetPacketInfo(buffer, buffer.Length, out _, out _, out _)
                .Should()
                .Be(PacketHandlers.PacketReadStatus.Incomplete);
        }
    }
}
