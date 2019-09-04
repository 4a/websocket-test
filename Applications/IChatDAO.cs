using System.Collections.Generic;

namespace WebSocketServer
{
    interface IChatDAO
    {
        List<Message> FetchLogs();
        void PutMessage();
    }
}