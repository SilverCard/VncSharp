namespace VncSharp.Messages
{
    public class SetPixelFormatMessage : MessageBase
    {
        [MessageMember(1)]
        public byte[] Padding { get; private set; }

        [MessageMember(2)]
        public PixelFormat PixelFormat { get; set; }

        public SetPixelFormatMessage() : base(0)
        {
            Padding = new byte[3];
            PixelFormat = new PixelFormat();
        }
    }
}
