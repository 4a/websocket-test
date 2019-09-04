using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketServer
{
    class Frame
    {
        public string Text { get; private set; }
        public bool IsFinal { get; private set; }
        public string Operation { get; private set; }
        private readonly UInt64 _payloadLength; // 7 bits, 7+16 bits, or 7+64 bits

        public Frame(byte[] bytes)
        {
            Operation = GetOperation(bytes);
            bool hasMask = ReadMaskBit(bytes);
            if (hasMask)
            {
                IsFinal = ReadFinBit(bytes);
                _payloadLength = GetPayloadLength(bytes);
                Text = Decode(bytes);
            }
            else
            {
                Operation = "close";
            }
        }

        private static bool ReadFinBit(byte[] bytes)
        {
            return ToBitString(bytes[0])[0] == '1' ? true : false;
        }

        private static bool ReadMaskBit(byte[] bytes)
        {
            return Convert.ToString(bytes[1], 2)[0] == '1' ? true : false;
        }

        private static UInt32 ReadLengthByte(byte[] bytes)
        {
            return (uint)(bytes[1] >= 128 ? bytes[1] - 128 : bytes[1]);
        }

        private static string GetOperation(byte[] bytes)
        {
            string opcodeNibble = ToBitString(bytes[0]).Substring(4);
            Dictionary<string, string> operations = new Dictionary<string, string>();
            // If an unknown opcode is received, the receiving endpoint MUST _Fail the WebSocket Connection_
            operations.Add("0000", "continuation");
            operations.Add("0001", "text");
            // operations.Add("0010", "binary"); // TODO: support binary data
            operations.Add("1000", "close");
            operations.Add("1001", "ping");
            operations.Add("1010", "pong");
            return operations.ContainsKey(opcodeNibble) ? operations[opcodeNibble] : "close";
        }

        private static int GetMaskPosition(byte[] bytes)
        {
            uint length = ReadLengthByte(bytes);
            if (length > 126)
            {
                return 10;
            }
            else if (length == 126)
            {
                return 4;
            }
            else
            {
                return 2;
            }
        }

        private UInt64 GetPayloadLength(byte[] bytes)
        {
            uint length = ReadLengthByte(bytes);
            if (length > 126)
            {
                byte[] subset = bytes.Skip(2).Take(8).ToArray();
                return BitConverter.ToUInt64(SwapBytes(subset));
            }
            else if (length == 126)
            {
                byte[] subset = bytes.Skip(2).Take(2).ToArray();
                return BitConverter.ToUInt16(SwapBytes(subset));
            }
            else
            {
                return length;
            }
        }

        private string Decode(byte[] bytes)
        {
            int maskStart = GetMaskPosition(bytes);
            int maskLength = 4;
            int messageStart = maskStart + maskLength;
            byte[] decoded = new byte[_payloadLength];
            byte[] encoded = bytes.Skip(messageStart).Take(bytes.Length - messageStart).ToArray();
            byte[] mask = bytes.Skip(maskStart).Take(maskLength).ToArray();
            for (int i = 0; i < encoded.Length; i++)
            {
                decoded[i] = (byte)(encoded[i] ^ mask[i % maskLength]);
            }
            return Encoding.UTF8.GetString(decoded);
        }

        public static string ToBitString(byte bits)
        {
            return Convert.ToString(bits, 2).PadLeft(8, '0');
        }

        public static byte[] SwapBytes(byte[] bytes)
        {
            return Enumerable.Reverse(bytes).ToArray();
        }
    }
}