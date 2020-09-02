using ChatServer.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatServer.Library
{
    public class Broadcaster
    {
        public int Id { get; set; }
        public ILogger Logger { get; set; }
        private Queue<KeyValuePair<ChatUser, string>> PendingMessages { get; set; }
        public Server Server { get; set; }
        public bool IsRunning { get; set; }
        public TcpListener Listener { get; set; }

        public Broadcaster()
        {
            PendingMessages = new Queue<KeyValuePair<ChatUser, string>>();
        }

        public void AddMessage(ChatUser user, string message)
        {
            PendingMessages.Enqueue(new KeyValuePair<ChatUser, string>(user, message));
        }

        public void Run()
        {
            IsRunning = true;
            while (IsRunning)
            {
                Logger.Log("Broadcaster {0} waiting for connection...", Id);
                TcpClient client = null;
                try
                {
                    client = Listener.AcceptTcpClient();

#if DEBUG == false
                    //timeouts affect debugging when stepping through code
                    client.ReceiveTimeout = 5000;
#endif

                }
                catch (Exception ex)
                {
                    Logger.Log("Broadcaster {0} could not accept TCP client: {1}", Id, ex.Message);
                    continue;
                }
                Logger.Log("Broadcaster {0} accepting client: {1}", Id, client.Client.RemoteEndPoint);

                BinaryReader reader = null;
                BinaryWriter writer = null;
                try
                {
                    BufferedStream stream = new BufferedStream(client.GetStream());
                    reader = new BinaryReader(stream);
                    writer = new BinaryWriter(stream);

                    //grab auth key
                    int authKeyLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    byte[] authKeyBytes = reader.ReadBytes(authKeyLength);
                    string authKey = Encoding.UTF8.GetString(authKeyBytes);

                    if (Server.ActiveUsers.ContainsKey(authKey) == true)
                    { 
                        while(true)
                        {
                            if(PendingMessages.Count > 0)
                            {
                                var message = PendingMessages.Dequeue();
                                byte[] userBytes = Encoding.UTF8.GetBytes(message.Key.Name);
                                writer.Write(IPAddress.HostToNetworkOrder(userBytes.Length));
                                writer.Write(userBytes);

                                byte[] messageBytes = Encoding.UTF8.GetBytes(message.Value);
                                writer.Write(IPAddress.HostToNetworkOrder(messageBytes.Length));
                                writer.Write(messageBytes);
                                stream.Flush();
                            }
                            else
                            {
                                Thread.Sleep(500);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Log("Broadcaster {0} exception handling client {1}, {2}: ", Id, client.Client.RemoteEndPoint, ex.Message);
                }
                finally
                {
                    Logger.Log("Broadcaster {0} done handling client {1}", Id, client.Client.RemoteEndPoint);
                    reader.Close();
                    writer.Close();
                    if (client != null)
                    {
                        client.Close();
                    }
                }
            }
        }
    }
}
