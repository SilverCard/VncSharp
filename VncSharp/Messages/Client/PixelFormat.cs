namespace VncSharp.Messages
{
    public class PixelFormat
    {
        [MessageMember(0)]
        public byte BitsPerPixel { get; set; }

        [MessageMember(1)]
        public byte Depth { get; set; }

        [MessageMember(2)]
        public byte BigEndianFlag { get; set; }

        [MessageMember(3)]
        public byte TrueColourFlag { get; set; }

        [MessageMember(4)]
        public ushort RedMax { get; set; }

        [MessageMember(5)]
        public ushort GreenMax { get; set; }

        [MessageMember(6)]
        public ushort BlueMax { get; set; }

        [MessageMember(7)]
        public byte RedShift { get; set; }

        [MessageMember(8)]
        public byte GreenShift { get; set; }

        [MessageMember(9)]
        public byte BlueShift { get; set; }

        [MessageMember(10)]
        public byte[] Padding { get; private set; }

        public PixelFormat()
        {
            Padding = new byte[3];
        }

    }
}
