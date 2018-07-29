using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VncSharp.Messages;

namespace VncSharp
{
    public static class BinarySerializer
    {

        private static void WriteInt32(Int32 n, Stream stream)
        {
            var b = BitConverter.GetBytes(n).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            WriteBytes(b, stream);
        }

        private static void WriteInt16(Int16 n, Stream stream)
        {
            var b = BitConverter.GetBytes(n).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            WriteBytes(b, stream);
        }

        private static void WriteUInt32(UInt32 n, Stream stream)
        {
            var b = BitConverter.GetBytes(n).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            WriteBytes(b, stream);
        }

        private static void WriteUInt16(UInt16 n, Stream stream)
        {
            var b = BitConverter.GetBytes(n).ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            WriteBytes(b, stream);
        }


        private static void WriteInt32Array(int[] value, Stream stream)
        {
            foreach (var v in value)
                WriteInt32(v, stream);
          
        }

        private static void WriteByte(byte b, Stream stream) => stream.WriteByte(b);
        private static void WriteBytes(byte[] b, Stream stream) => stream.Write(b, 0, b.Length);


        public static void Serialize(Object obj, Stream stream)
        {

            var memberInfos = obj.GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(MessageMemberAttribute))).Select(p => MessageMemberInfo.FromPropertyInfo(p, obj));

            foreach (var m in memberInfos.OrderBy(x => x.MessageMemberAttribute.Index))
            {
                if(m.Type == typeof(byte))
                {
                    WriteByte((byte)m.Value, stream);
                }
                else if (m.Type == typeof(byte[]))
                {
                    WriteBytes((byte[])m.Value, stream);
                }
                else if (m.Type == typeof(short))
                {
                    WriteInt16((short)m.Value, stream);
                }
                else if (m.Type == typeof(int))
                {
                    WriteInt32((int)m.Value, stream);
                }
                else if (m.Type == typeof(ushort))
                {
                    WriteUInt16((ushort)m.Value, stream);
                }
                else if (m.Type == typeof(uint))
                {
                    WriteUInt32((uint)m.Value, stream);
                }
                else if (m.Type == typeof(int[]))
                {
                    WriteInt32Array((int[])m.Value, stream);
                }
                else if (m.Type == typeof(bool))
                {
                    WriteBool((bool)m.Value, stream);
                }
                else
                {
                    Serialize(m.Value, stream);
                }

            }

            stream.Flush();
        }

        private static void WriteBool(bool value, Stream stream)
        {
            byte v = (byte)(value ? 1 : 0);
            WriteByte(v, stream);
        }
    }
}
