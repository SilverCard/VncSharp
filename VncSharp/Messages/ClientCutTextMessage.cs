using System;
using System.Text;

namespace VncSharp.Messages
{
    public class ClientCutTextMessage : MessageBase
    {
        public static readonly Encoding Encoding = Encoding.GetEncoding("ISO-8859-1");

        [MessageMember(1)]
        public byte[] Padding { get; private set; }

        [MessageMember(2)]
        public uint Length { get; private set; }

        [MessageMember(3)]
        public byte[] Bytes { get; private set; }

        public ClientCutTextMessage(String text) : base(6)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            Padding = new byte[3];

            Bytes = Encoding.GetBytes(text);
            Length = (uint)Bytes.Length;
        }
    }
}
