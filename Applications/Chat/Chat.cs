using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json;

using WebSocketServer.Applications;
using WebSocketServer.Applications.Chat;
namespace WebSocketServer
{
    class Chat : IApplication
    {
        public static Chat Instance { get => _instance; } // TODO: is this threadsafe?
        private static readonly Chat _instance = new Chat();
        private Chat()
        {

        }

        public void HandleMessage(ConcurrentDictionary<String, Connection> connections, Message message)
        {
            LogMessage(message);
            WriteTextToAll(connections, message);
        }

        public static void FetchLogs(Connection connection, int from, int to)
        {
            IDataAccessObject dao = new MySqlDao();
            List<Message> messages = dao.Fetch(from, to);
            foreach (Message message in messages)
            {
                connection.WriteText(message);
            }
        }

        private static void LogMessage(Message message)
        {
            IDataAccessObject dao = new MySqlDao();
            Model data = ParseJSON(message.Text);
            dao.Insert(data);
        }

        private static void WriteTextToAll(ConcurrentDictionary<String, Connection> connections, Message message) // TODO: is this threadsafe?
        {
            foreach (KeyValuePair<string, Connection> item in connections)
            {
                Connection connection = item.Value;
                if (connection.App == _instance)
                {
                    connection.WriteText(message);
                }
            }
        }

        private static Model ParseJSON(string json)
        {
            Console.WriteLine(json);
            return JsonConvert.DeserializeObject<Model>(json);
        }
    }
}