using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using WebSocketServer.Applications;
namespace WebSocketServer
{
    class Server // TODO: implement IObservable?
    {
        public bool IsActive { get; private set; }
        public ConcurrentDictionary<String, Connection> Connections { get; private set; } = new ConcurrentDictionary<String, Connection>();
        private IPAddress _ip;
        private int _port;

        public Server(string ip, int port)
        {
            _ip = IPAddress.Parse(ip);
            _port = port;
            IsActive = true;
        }

        public void Listen()
        {
            TcpListener listener = new TcpListener(_ip, _port);
            listener.Start();
            Console.WriteLine($"Server has started on {_ip}:{_port}.");
            Console.WriteLine("Waiting for a connection...", Environment.NewLine);
            StartHeartBeat();
            while (IsActive)
            {
                Connection connection = new Connection(this, listener.AcceptTcpClient()); // blocks until a client connects
                connection.StreamInput(); // loops on a new thread
                AddConnection(connection);
            }

        }

        public void Stop()
        {
            foreach (KeyValuePair<string, Connection> item in Connections) // TODO: is this threadsafe?
            {
                Connection connection = item.Value;
                RemoveConnection(connection);
            }
            IsActive = false;
        }

        private Connection AddConnection(Connection connection)
        {
            while (!connection.HandWasShook) ;

            string key = connection.Key;
            bool added = Connections.TryAdd(key, connection); // TODO: is this threadsafe?

            if (added)
            {
                Console.WriteLine($"Successfully added the connection at key: {key}");
            }
            else
            {
                RemoveConnection(connection);
                Console.WriteLine($"Could not add the connection at key: {key}");
            }
            Console.WriteLine($"Current connections: {Connections.Count}");

            return connection;
        }

        public void RemoveConnection(Connection connection)
        {
            Connections.Remove(connection.Key, out _); // TODO: is this threadsafe?
            Console.WriteLine($"Released the connection at key: {connection.Key}");
            Console.WriteLine($"Current connections: {Connections.Count}");
        }

        public void HandleMessage(IApplication app, Message message)
        {
            app.HandleMessage(Connections, message);
        }

        private void StartHeartBeat()
        {
            Task task = Task.Run(() =>
            {
                while (IsActive)
                {
                    Thread.Sleep(10000);
                    foreach (KeyValuePair<string, Connection> item in Connections) // TODO: is this threadsafe?
                    {
                        Connection connection = item.Value;
                        connection.WritePing();
                    }
                }
            });
        }
    }
}