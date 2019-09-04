using System;
using System.Collections.Concurrent;
using MySql.Data;
using MySql.Data.MySqlClient;
using DotNetEnv;

namespace WebSocketServer
{
    class DBConnection
    {
        private ConcurrentBag<MySqlConnection> _pool = new ConcurrentBag<MySqlConnection>();
        private int _currentPoolSize = 0;
        private int _maximumPoolSize = 2;
        private string _server;
        private string _database;
        private string _user;
        private string _pass;
        public static DBConnection Instance { get => _instance; } // TODO: is this threadsafe?
        private static readonly DBConnection _instance = new DBConnection();

        private DBConnection()
        {
            Env.Load();
            _server = Env.GetString("DB_HOST");
            _database = Env.GetString("DB_NAME");
            _user = Env.GetString("DB_USER");
            _pass = Env.GetString("DB_PASS");
        }

        public MySqlConnection Connect()
        {
            Console.WriteLine($"Pool Size: {_pool.Count}");
            MySqlConnection connection = null;
            if (_pool.Count == 0 && _currentPoolSize < _maximumPoolSize)
            {
                Console.WriteLine("Opening new db connection");
                _currentPoolSize += 1;
                connection = new MySqlConnection(
                    $"Server={_server};" +
                    $"database={_database}; " +
                    $"UID={_user}; " +
                    $"password={_pass}"
                );
                connection.Open();
                // _pool.Add(connection);
            }
            else
            {
                Console.WriteLine("Using db connection from pool");
                _pool.TryTake(out connection);
            }
            Console.WriteLine($"Pool Size: {_pool.Count}");
            return connection;
        }

        public void Close(MySqlConnection connection)
        {
            if (connection != null)
            {
                _pool.Add(connection);
            }
            Console.WriteLine($"Pool Size: {_pool.Count}");
        }

        private void Stop()
        {
            foreach (MySqlConnection connection in _pool)
            {
                connection.Close();
            }
        }
    }
}