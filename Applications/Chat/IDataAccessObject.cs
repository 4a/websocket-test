using System.Collections.Generic;

using WebSocketServer.Applications.Chat;
namespace WebSocketServer
{
    interface IDataAccessObject
    {
        List<Message> Fetch(int from, int to);
        int Insert(Model data);
    }
}