using ChatServer.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer.Library
{
    public class Receiver
    {
        public int ListenPort { get; set; }
        public bool IsRunning { get; private set; }
        public ILogger Logger { get; set; }
        public Server Server { get; set; }

        public Receiver()
        {
            
        }

        public void Run()
        {

            IsRunning = true;
            TcpListener listener = new TcpListener(IPAddress.Any, ListenPort);
            listener.Start();
            while (IsRunning)
            {
                Logger.Log("Receiver waiting for connection on port {0}...", ListenPort);
                TcpClient client = null;
                try
                {
                    client = listener.AcceptTcpClient();

#if DEBUG == false
                    //timeouts affect debugging when stepping through code
                    client.ReceiveTimeout = 5000;
#endif

                }
                catch (Exception ex)
                {
                    Logger.Log("Could not accept TCP client: {0}", ex.Message);
                    continue;
                }
                Logger.Log("Receiver accepting client: {0}", client.Client.RemoteEndPoint);

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

                    if(Server.ActiveUsers.ContainsKey(authKey) == true)
                    {
                        //grab message
                        int messageLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                        byte[] messageBytes = reader.ReadBytes(messageLength);
                        string message = Encoding.UTF8.GetString(messageBytes);
                        Server.MessageReceived(Server.ActiveUsers[authKey], message);

                        Logger.Log("{0} says: {1}", Server.ActiveUsers[authKey].Name, message);
                    }

                }
                catch (Exception ex)
                {
                    Logger.Log("Receiver exception handling client {0}, {1}: ", client.Client.RemoteEndPoint, ex.Message);
                }
                finally
                {
                    Logger.Log("Receiver done handling client {0}", client.Client.RemoteEndPoint);
                    reader.Close();
                    writer.Close();
                }
            }
        }
    }
}
