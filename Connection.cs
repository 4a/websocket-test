using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using DotNetEnv;

using WebSocketServer.Applications;
namespace WebSocketServer
{
    class Connection
    {
        public string Key { get; private set; }
        public TcpClient Client { get; private set; }
        public bool HandWasShook { get; private set; } = false;
        public IApplication App { get; private set; }
        private Server _server;
        private NetworkStream _stream;
        private bool _isClosed = false;
        private Message _latestMessage;
        private bool _buildingMessage = false;
        private Dictionary<string, string> AppURI = new Dictionary<string, string>();

        public Connection(Server server, TcpClient tcpClient)
        {
            _server = server;
            Client = tcpClient;

            Env.Load();
            AppURI.Add("chat", Env.GetString("APP_CHAT_URI"));

            Console.WriteLine("A client connected.");
        }

        public void StreamInput()
        {
            Task task = Task.Run(() =>
            {
                using (_stream = Client.GetStream())
                {
                    while (!_isClosed)
                    {
                        BlockUntilReady(_stream); // blocks until 3 bytes are available

                        byte[] bytes = new byte[0];
                        while (Client.Available > 0)
                        {
                            byte[] byteChunk = new byte[Client.Available];
                            _stream.Read(byteChunk, 0, byteChunk.Length); // blocks until client input received
                            int bytesLength = bytes.Length;
                            Array.Resize(ref bytes, bytesLength + byteChunk.Length);
                            Array.Copy(byteChunk, 0, bytes, bytesLength, byteChunk.Length);
                        }

                        if (!HandWasShook)
                        {
                            HandShake(_stream, bytes);
                            string origin = GetOrigin(bytes);
                            SetApplication(origin);
                        }
                        else
                        {
                            Frame frame = new Frame(bytes);
                            HandleFrame(frame);
                        }
                    }
                }
                Disconnect();
            });
        }

        public void WriteText(Message message)
        {
            bool sent = TryWriteToStream(message.ByteData);
            if (sent)
            {
                Console.WriteLine($"Successfully sent `TEXT` to the client at key: {Key}");
                Console.WriteLine($"Text length: {message.Text.Length}");
            }
        }

        public void WritePing()
        {
            byte opcodeByte = Convert.ToByte("10001001", 2); // FIN (Bit 0) | Opcode (Bit 4:7) | 1001: "ping"
            bool sent = TryWriteToStream(new byte[] { opcodeByte, 0, 0, 0 });
            if (sent)
            {
                Console.WriteLine($"Successfully sent `PING` to the client at key: {Key}");
                // AwaitPong();
            }
        }

        private void WritePong()
        {
            byte opcodeByte = Convert.ToByte("10001010", 2); // FIN (Bit 0) | Opcode (Bit 4:7) | 1010: "pong"
            bool sent = TryWriteToStream(new byte[] { opcodeByte, 0, 0 });
            Console.WriteLine($"Received `PING` from the client at key: {Key}");
            if (sent)
            {
                Console.WriteLine($"Successfully sent `PONG` to the client at key: {Key}");
            }
        }

        private void WriteClose()
        {
            Console.WriteLine($"Attempting to send `CLOSE` to the client at key: {Key}");

            byte opcodeByte = Convert.ToByte("10001000", 2); // FIN (Bit 0) | Opcode (Bit 4:7) | 1000: "close"
            bool sent = TryWriteToStream(new byte[] { opcodeByte, 0, 0 });
            if (sent)
            {
                Disconnect();
                Console.WriteLine($"Successfully sent `CLOSE` to the client at key: {Key}");
            }
        }

        private bool TryWriteToStream(byte[] output)
        {
            bool success = false;
            try
            {
                _stream.Write(output, 0, output.Length);
                success = true;
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine($"Failed to send `CLOSE` event! Could not find the client at key: {Key}.");
                Disconnect();
            }
            catch (Exception ex) // FIXME: probably want to do something else here
            {
                Console.WriteLine(ex);
            }
            return success;
        }

        private void Disconnect()
        {
            Console.WriteLine($"Disconnecting the client at key: {Key}");
            _isClosed = true;
            _stream.Dispose(); // TODO: is it good enough to just call Dispose?
            _server.RemoveConnection(this);
        }

        private void BlockUntilReady(NetworkStream stream)
        {
            while (!stream.DataAvailable) ;
            while (Client.Available < 3) ;
        }

        private void HandShake(NetworkStream stream, byte[] bytes)
        {
            string request = Encoding.UTF8.GetString(bytes);
            if (Regex.IsMatch(request, "^GET"))
            {
                string key = new Regex("Sec-WebSocket-Key: (.*)").Match(request).Groups[1].Value.Trim();
                string acceptKey = AcceptKey(key);
                string EOL = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                string response = (
                    "HTTP/1.1 101 Switching Protocols" + EOL +
                    "Connection: Upgrade" + EOL +
                    "Upgrade: websocket" + EOL +
                    "Sec-WebSocket-Accept: " + acceptKey + EOL + EOL
                );
                byte[] data = Encoding.UTF8.GetBytes(response);

                stream.Write(data, 0, response.Length);
                HandWasShook = true;
                Key = acceptKey; // TODO: look into a better way to generate keys
                Console.WriteLine("Handshake complete.");
            }
            else
            {
                Console.WriteLine("Handshake failed, disconnecting...");
                Disconnect();
            }
        }

        private static string AcceptKey(string key)
        {
            string GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"; // https://tools.ietf.org/html/rfc4122
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA1.Create().ComputeHash(
                    Encoding.UTF8.GetBytes(key + GUID)
                )
            );
        }

        private string GetOrigin(byte[] bytes)
        {
            string request = Encoding.UTF8.GetString(bytes);
            Regex regex = new Regex(@"(?<=Origin:\s)(?<origin>[^\s]*)"); // matches any non whitespace char after Origin
            return Convert.ToString(regex.Match(request));
        }

        private void SetApplication(string origin)
        {
            if (origin == AppURI["chat"])
            {
                App = Chat.Instance;
                Chat.FetchLogs(this, 0, 50);
            }
            else
            {
                Console.WriteLine("Unknown application, disconnecting...");
                Disconnect();
            }
        }

        private void HandleFrame(Frame frame)
        {
            switch (frame.Operation)
            {
                case "text":
                case "continuation":
                    BuildMessage(frame);
                    break;
                case "ping":
                    WritePong();
                    break;
                case "pong":
                    HandlePong();
                    break;
                default:
                    WriteClose();
                    break;
            }
        }

        private void BuildMessage(Frame frame)
        {
            if (!_buildingMessage)
            {
                _buildingMessage = true;
                _latestMessage = new Message(frame.Text);
            }
            else
            {
                _latestMessage.AppendText(frame.Text);
            }

            if (frame.IsFinal)
            {
                _buildingMessage = false;
                _server.HandleMessage(App, _latestMessage);
            }
        }

        private void HandlePong()
        {
            Console.WriteLine($"Received `PONG` from the client at key: {Key}");
        }
    }
}