using ChatServer.Library.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatServer.Library
{
    public class Server
    {
        public Dictionary<string, ChatUser> ActiveUsers { get; set; }
        public ILogger Logger
        {
            get
            {
                return _authenticator.Logger;
            }
            set
            {
                _authenticator.Logger = value;
                _receiver.Logger = value;
                foreach (var broadcaster in _broadcasters)
                {
                    broadcaster.Logger = value;
                }
            }
        }

        public int AuthenticationPort 
        { 
            get 
            {
                return _authenticator.ListenPort;
            } 
            set 
            {
                _authenticator.ListenPort = value;
            } 
        }
        public int ReceivePort
        {
            get
            {
                return _receiver.ListenPort;
            }
            set
            {
                _receiver.ListenPort = value;
            }
        }
        public int BroadcastPort { get; set; }
        public int MaxConcurrentUsers { get; private set; }
        private static Server _instance = null;
        private Authenticator _authenticator = new Authenticator();
        private Receiver _receiver = new Receiver();
        private List<Broadcaster> _broadcasters = new List<Broadcaster>();
        private Thread _authenticatorThread = null;
        private Thread _receiverThread = null;
        private List<Thread> _broadcasterThreads = new List<Thread>();


        private Server(int maxConcurrentUsers = 40)
        {
            ActiveUsers = new Dictionary<string, ChatUser>();
            MaxConcurrentUsers = maxConcurrentUsers;
            for (int i = 0; i < MaxConcurrentUsers; i++)
            {
                _broadcasters.Add(new Broadcaster());
                _broadcasters[i].Id = i;
                _broadcasters[i].Server = this;
            }

            _authenticator.Server = this;
            _authenticator.ListenPort = AuthenticationPort;

            _receiver.Server = this;
            _receiver.ListenPort = ReceivePort;
        }

        public void MessageReceived(ChatUser user, string message)
        {
            foreach (var broadcaster in _broadcasters)
            {
                broadcaster.AddMessage(user, message);
            }
        }

        public void Run()
        {
            ThreadStart authStart = _authenticator.Run;
            _authenticatorThread = new Thread(authStart);
            _authenticatorThread.Start();

            ThreadStart receiverStart = _receiver.Run;
            _receiverThread = new Thread(receiverStart);
            _receiverThread.Start();

            TcpListener listener = new TcpListener(IPAddress.Any, BroadcastPort);
            listener.Start();
            foreach (var broadcaster in _broadcasters)
            {
                broadcaster.Listener = listener;
                ThreadStart broadcastStart = broadcaster.Run;
                Thread current = new Thread(broadcastStart);
                current.Start();
                _broadcasterThreads.Add(current);
            }
        }

        public static Server GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Server();
            }
            return _instance;
        }
    }
}
