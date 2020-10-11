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
        static bool serverstarted = false;
        static void Main(string[] args)
        {
            if (!serverstarted)
            {
                serverstarted = true;
                ServerManager sm = new ServerManager();
                sm.StartServer(3001);
            }
        }
    }

    class ServerManager
    {
        private NetServer server_connection = null;
        //private long latency = 0;
        // int is client number, bool is value
        //public Dictionary<int, bool> clients = new Dictionary<int, bool>();
        private int c1 = 0;
        private int c2 = 0;

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
            try
            {
                NetOutgoingMessage msg = server_connection.CreateMessage();
                msg.Write(data);
                if (server_connection.Connections.Count > 0)
                    server_connection.SendMessage(msg, server_connection.Connections[client], NetDeliveryMethod.ReliableOrdered);
                else
                    Console.WriteLine("Client not connected");
            }
            catch(Exception e)
            {
                Console.WriteLine("sendData error: " + e.ToString());
            }
        }

        /*
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
        */

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
                if (server_connection.ConnectionsCount > 1)
                {
                    sendData("start", 0);
                    sendData("start", 1);
                }
                else
                {
                    Console.WriteLine("Not Enough Players! Amount of Players: " + server_connection.ConnectionsCount);
                }
            }
            else if(message == "join")
            {
                //clients = null;
                try
                {
                    sendData("wholeft", 0);
                    sendData("wholeft", 1);
                }
                catch (Exception e)
                {
                    Console.WriteLine("error lol: " + e.ToString());
                }
            }
            else if(message == "imhere")
            {
                Console.WriteLine("spit");
                // whoever is left, set them as host
                if(server_connection.ConnectionsCount <= 1)
                {
                    // there's no one so theyre the host
                    Console.WriteLine("a1");
                    //clients.Add(0, true);
                    Console.WriteLine("a2");
                    sendData("identifier,0", 0);
                }
                else
                {
                    // theres one person so theyre the player
                    Console.WriteLine("b1");
                    //clients.Add(1, true);
                    Console.WriteLine("b2");
                    sendData("identifier,1", 1);
                }
            }
            else
            {
                if (server_connection.ConnectionsCount > 1)
                {
                    try
                    {
                        // split data
                        string[] data = message.Split(';');
                        string newdata = "";
                        foreach (string n in data)
                        {
                            if (n == "0" || n == "1") { }
                            else
                            {
                                // its not a client identifier
                                newdata = n + ";";
                            }
                        }
                        // this is debug, remove if u want
                        Console.WriteLine(newdata);
                        // end of debug
                        // send data
                        if (data[0] == "0")
                        {
                            // send it to the player
                            sendData(newdata, 1);
                        }
                        else if (data[0] == "1")
                        {
                            sendData(newdata, 0);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("error lol: " + e.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Not Enough Players! Amount of Players: " + server_connection.ConnectionsCount);
                }
            }
        }

        void ListenerLoop()
        {
            do
            {
                // check to see if a client left
                int connections = server_connection.ConnectionsCount;
                c2 = c1;
                c1 = connections;

                if(c1 != c2 && c2 >= c1)
                {
                    Console.WriteLine("A Player Left " + c1 + c2);
                    // someone left
                    ///*
                    //clients = null;
                    try
                    {
                        sendData("wholeft", 0);
                        sendData("wholeft", 1);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("error lol: " + e.ToString());
                    }
                    //*/
                }

                // check if player joined
                if(c2 < c1)
                {
                    Console.WriteLine("Player Joined");
                    try
                    {
                        sendData("wholeft", 0);
                        sendData("wholeft", 1);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("error lol: " + e.ToString());
                    }
                }
                // this keeps the server running
                NetIncomingMessage message;
                if ((message = server_connection.ReadMessage()) != null)
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
