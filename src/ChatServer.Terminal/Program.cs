using ChatServer.Library;
using ChatServer.Library.Logging;
using System;
using System.Threading;

namespace ChatServer.Terminal
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = Server.GetInstance();
            server.Logger = new ConsoleLogger();
            server.AuthenticationPort = 3461;
            server.ReceivePort = 3462;
            server.BroadcastPort = 3463;
            server.Run();

            while(true)
            {
                Thread.Sleep(100);
            }
        }
    }
}
