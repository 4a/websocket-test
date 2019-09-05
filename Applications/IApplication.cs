using System;
using System.Collections.Concurrent;

namespace WebSocketServer.Applications
{
    interface IApplication
    {
        void HandleMessage(ConcurrentDictionary<String, Connection> connections, Message message);
    }
}