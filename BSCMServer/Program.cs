using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace BSCMServer
{
    class Program
    {

        static void Main(string[] args)
        {
            ServerManager sm = new ServerManager();
            sm.StartServer(3001);
        }
    }

    class ServerManager
    {
        private NetServer server_connection = null;
        //private long latency = 0;
        // int is client number, bool is value
        private Dictionary<int, bool> clients;

        public void StartServer(int port)
        {
            try
            {
                NetPeerConfiguration config = new NetPeerConfiguration("BSCM");
                config.Port = port;
                server_connection = new NetServer(config);
                server_connection.Start();
                Console.WriteLine("Server started on Port " + port);
                ListenerLoop();
            }
            catch
            {
                Console.WriteLine("Error while starting Server");
            }
        }

        private long getTimestamp()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
        }

        private void sendData(string data, int client)
        {
            NetOutgoingMessage msg = server_connection.CreateMessage();
            msg.Write(data);
            if (server_connection.Connections.Count > 0)
                server_connection.SendMessage(msg, server_connection.Connections[client], NetDeliveryMethod.ReliableOrdered);
            else
                Console.WriteLine("Client not connected");
        }

        int ReturnPlayerType(string type)
        {
            // check for host or player
            foreach(KeyValuePair<int, bool> entry in clients)
            {
                if(type == "host")
                {
                    if(entry.Value == true)
                    {
                        return entry.Key;
                    }
                }
                else if(type == "player")
                {
                    if(entry.Value == false)
                    {
                        return entry.Key;
                    }
                }
            }
            return 0;
        }

        void parseMessage(string message)
        {
            if (message == "ready")
            {
                // Ping will need re-write
                // when client is ready
                //latency = getTimestamp();
                //sendData("ping");
                //Console.WriteLine("Sending ping");
            }
            else if (message == "pong")
            {
                // when client replies to pong
                //latency = getTimestamp() - latency;
                //Console.WriteLine("Client ready");
            }
            else if (message == "start")
            {
                sendData("start", 0);
                sendData("start", 1);
            }
            else if(message == "join")
            {
                if(clients.Count <= 0)
                {
                    // this guy is host
                    clients.Add(0, true);
                    sendData("identifier,0", 0);
                }
                else
                {
                    // they aren't the host
                    clients.Add(1, false);
                    sendData("identifier,1", 1);
                }
            }
            else
            {
                // split data
                string[] data = message.Split(';');
                string newdata = "";
                foreach (string n in data)
                {
                    if(n == "0" || n == "1") { } else
                    {
                        // its not a client identifier
                        newdata = n + ";";
                    }
                }
                // this is debug, remove if u want
                Console.WriteLine(newdata);
                // end of debug
                // send data
                if(data[0] == "0")
                {
                    // send it to the player
                    sendData(newdata, 1);
                }
                else if(data[0] == "1")
                {
                    sendData(newdata, 0);
                }
            }
        }

        void ListenerLoop()
        {
            do
            {
                // this keeps the server running
                NetIncomingMessage message;
                while ((message = server_connection.ReadMessage()) != null)
                {
                    if (message.MessageType == NetIncomingMessageType.Data)
                        parseMessage(message.ReadString());
                    else
                        Console.WriteLine(message.MessageType.ToString() + ":" + message.ReadString());
                }
            }
            while (true);
        }
    }
}
