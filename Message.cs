using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketServer
{
    class Message
    {
        public string Text { get; private set; }
        public byte[] ByteData { get; private set; }
        public string Source { get; private set; }

        public Message(string text)
        {
            Text = text;
            Source = "/chat";
            ByteData = GetByteData();
        }

        public void AppendText(string text)
        {
            Text += text;
            ByteData = GetByteData();
        }

        private byte[] GetByteData()
        {
            byte opcode = GetOpcode();
            byte[] payload = GetPayload();
            byte[] payloadLength = GetPayloadLength();
            return new byte[] { opcode }.Concat(payloadLength).Concat(payload).ToArray();
        }

        private byte GetOpcode()
        {
            return Convert.ToByte("10000001", 2); // FIN (Bit 0) | Opcode (Bit 4:7) | 0001: "text"
        }

        private byte[] GetPayload()
        {
            return Encoding.UTF8.GetBytes(Text);
        }

        private byte[] GetPayloadLength() // big endian
        {
            byte[] payloadLength;
            if (Text.Length <= 125)
            {
                byte length = (byte)Text.Length;
                payloadLength = new byte[] { length };
            }
            else if (Text.Length < Math.Pow(2, 16))
            {
                IEnumerable<byte> extendedLength = Enumerable.Reverse(BitConverter.GetBytes((UInt16)Text.Length));
                payloadLength = (new byte[] { 126 }).Concat(extendedLength).ToArray();
            }
            else
            {
                IEnumerable<byte> extendedLength = Enumerable.Reverse(BitConverter.GetBytes((UInt64)Text.Length));
                payloadLength = (new byte[] { 127 }).Concat(extendedLength).ToArray();
            }
            return payloadLength;
        }
    }
}