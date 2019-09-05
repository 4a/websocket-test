using MySql.Data.MySqlClient;
using DotNetEnv;

namespace WebSocketServer
{
    class MySql
    {
        private string _connector;
        public static MySql Instance { get => _instance; }
        private static readonly MySql _instance = new MySql();

        private MySql()
        {
            Env.Load();
            _connector = (
                $"Server={Env.GetString("DB_HOST")};" +
                $"database={Env.GetString("DB_NAME")}; " +
                $"UID={Env.GetString("DB_USER")}; " +
                $"password={Env.GetString("DB_PASS")}"
            );
        }

        public MySqlConnection GetConnection()
        {
            /**
            * https://dev.mysql.com/doc/connector-net/en/connector-net-programming-connection-pooling.html
            * To work as designed, it is best to let the connection pooling system manage all connections. 
            * Do not create a globally accessible instance of MySqlConnection and then manually open and close it. 
            * This interferes with the way the pooling works and can lead to unpredictable results or even exceptions.
            */
            string connectionString = (_connector);
            MySqlConnection connection = new MySqlConnection(connectionString);
            return connection;
        }
    }
}