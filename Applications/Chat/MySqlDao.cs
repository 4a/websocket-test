using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

using WebSocketServer.Applications.Chat;
namespace WebSocketServer
{
    class MySqlDao : IDataAccessObject
    {
        public List<Message> Fetch(int from, int to)
        {
            var result = new List<Message>();
            string query = (
                $"SELECT username, message, UNIX_TIMESTAMP(date) FROM chatlog " +
                $"ORDER BY id DESC LIMIT {to} OFFSET {from}"
            );

            MySqlConnection connection = MySql.Instance.GetConnection();
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
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
                    result.Add(message);
                }
                reader.Close();
                result = Enumerable.Reverse(result).ToList();
            }
            finally
            {
                connection.Close();
            }
            return result;
        }

        public int Insert(Model data)
        {
            int rowsUpdated = 0;
            string query = (
                $"INSERT INTO chatlog (username, message, date) " +
                $"VALUES ( '{data.Username}', '{data.Message}', FROM_UNIXTIME({data.Date / 1000}) )"
            );

            MySqlConnection connection = MySql.Instance.GetConnection();
            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                rowsUpdated = command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
            return rowsUpdated;
        }
    }
}