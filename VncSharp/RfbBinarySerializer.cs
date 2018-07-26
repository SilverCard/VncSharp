using System;
using System.Linq;

namespace VncSharp
{
    public class RfbBinarySerializer
    {

        public static byte[] Serialize(Int16 n)
        {
            var b = BitConverter.GetBytes(n).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return b;
        }

        public static byte[] Serialize(Int32 n)
        {
            var b = BitConverter.GetBytes(n).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return b;
        }

        public static byte[] Serialize(UInt16 n)
        {
            var b = BitConverter.GetBytes(n).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return b;
        }

        public static byte[] Serialize(UInt32 n)
        {
            var b = BitConverter.GetBytes(n).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return b;
        }

        public static void SerializeCopy(Int16 n, byte[] array, int idx) => Serialize(n).CopyTo(array, idx);
        public static void SerializeCopy(Int32 n, byte[] array, int idx) => Serialize(n).CopyTo(array, idx);
        public static void SerializeCopy(UInt16 n, byte[] array, int idx) => Serialize(n).CopyTo(array, idx);
        public static void SerializeCopy(UInt32 n, byte[] array, int idx) => Serialize(n).CopyTo(array, idx);

    }
}
