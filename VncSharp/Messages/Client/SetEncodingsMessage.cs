using System;
using System.Linq;

namespace VncSharp.Messages
{
    public class SetEncodingsMessage : MessageBase
    {
        [MessageMember(1)]
        public byte Padding { get; private set; }

        [MessageMember(2)]
        public short NumberOfEncodings { get; private set; }

        [MessageMember(3)]
        public int[] Encodings { get; private set; }

        public SetEncodingsMessage(RfbEncodingType[] encodings) : base(2)
        {
            if (encodings == null) throw new ArgumentNullException(nameof(encodings));
            if (encodings.Length > short.MaxValue) throw new ArgumentOutOfRangeException("Too many encodings.");

            NumberOfEncodings = (short)encodings.Length;
            Encodings = encodings.Select(e => (int)e).ToArray();
        }
    }
}
