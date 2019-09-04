using System;
using System.Threading.Tasks;
using DotNetEnv;

namespace WebSocketServer
{
    class Entry
    {
        public static void Main()
        {
            Env.Load();
            string host = Env.GetString("WSS_HOST");
            int port = Env.GetInt("WSS_PORT");
            Server server = new Server(host, port);

            Task task = Task.Run(() =>
            {
                server.Listen();
            });

            Console.ReadLine(); // TODO: find a better way to exit
            server.Stop();
        }
    }
}
