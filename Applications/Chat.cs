using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using MySql.Data;
using MySql.Data.MySqlClient;

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
            LogMessageToDB(message);
            WriteTextToAll(connections, message);
        }

        public static void DisplayHistory(Connection connection)
        {
            List<Message> messages = FetchLogsFromDB(connection);
            foreach (Message message in messages)
            {
                connection.WriteText(message);
            }
        }

        private static List<Message> FetchLogsFromDB(Connection connection) // TODO: is this threadsafe?
        {
            List<Message> output = new List<Message>();

            DBConnection db = DBConnection.Instance;
            MySqlConnection dbConn = db.Connect();
            if (dbConn != null)
            {
                string query = (
                    $"SELECT username, message, UNIX_TIMESTAMP(date) FROM chatlog " +
                    $"ORDER BY id DESC LIMIT 50"
                );
                MySqlCommand command = new MySqlCommand(query, dbConn);
                MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string json = (
                        "{" +
                            $"\"username\": \"{reader.GetString(0)}\", " +
                            $"\"message\": \"{reader.GetString(1)}\", " +
                            $"\"date\": {reader.GetUInt64(2) * 1000}" +
                        "}"
                    );
                    Message message = new Message(json);
                    output.Add(message);
                }
                reader.Close();
                db.Close(dbConn);
                output = Enumerable.Reverse(output).ToList();
            }

            return output;
        }

        private static void LogMessageToDB(Message message) // TODO: is this threadsafe?
        {
            MessageData data = ParseJSON(message.Text);
            DBConnection db = DBConnection.Instance;
            MySqlConnection dbConn = db.Connect();
            if (dbConn != null)
            {
                string query = (
                    $"INSERT INTO chatlog (username, message, date) " +
                    $"VALUES ( '{data.Username}', '{data.Message}', FROM_UNIXTIME({data.Date / 1000}) )"
                );
                MySqlCommand command = new MySqlCommand(query, dbConn);
                int rowsUpdated = command.ExecuteNonQuery();
                db.Close(dbConn);
            }
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

        private static MessageData ParseJSON(string json)
        {
            Console.WriteLine(json);
            return JsonConvert.DeserializeObject<MessageData>(json);
        }

        private class MessageData
        {
            [JsonProperty("username")]
            public string Username { get; private set; }
            [JsonProperty("message")]
            public string Message { get; private set; }
            [JsonProperty("date")]
            public long Date { get; private set; }
        }
    }
}