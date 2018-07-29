using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VncSharp.Messages;

namespace VncSharp.Tests
{
    [TestClass]
    public class SerializeTest
    {
        private byte[] MessageToBytes(Object obj)
        {
            using (var ms = new MemoryStream())
            {
                BinarySerializer.Serialize(obj, ms);
                return ms.ToArray();
            }
        }

        [TestMethod]
        public void Serialize_FramebufferUpdateRequestMessage()
        {
            var message = new FramebufferUpdateRequestMessage()
            {
                X = 1,
                Y = 2,
                Width = 3,
                Height = 4
            };
            var bytes = new byte[]
            {
                3, 0,
                0, 1,
                0, 2,
                0, 3,
                0, 4,
            };

            var messageBytes = MessageToBytes(message);
            CollectionAssert.AreEqual(bytes, messageBytes);
        }
    }
}
