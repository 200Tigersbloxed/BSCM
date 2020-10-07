using System;
using Lidgren.Network;
using UnityEngine;
using System.Linq;
using System.Threading;

namespace BSCM
{
    public class Multiplayer
    {
        private NetClient client_connection = null;
        private NetServer server_connection = null;
        private Vector3 latestPosition;
        private Quaternion latestRotation;
        private ScoreController ScoreController;
        private SongController SongController;
        private bool clientReady = false;
        private long latency = 0;
        private int clientIdentifier = 0;

        public Multiplayer()
        {
            try
            {
                var config = new NetPeerConfiguration("BSCM");
                client_connection = new NetClient(config);
                client_connection.Start();
                client_connection.Connect(host: PluginConfig.Instance.url, port: PluginConfig.Instance.port);
                Thread.Sleep(1000);
                Plugin.Log.Info("Client Ready");
            }
            catch
            {
                Plugin.Log.Error("Error while connecting to Server");
            }
            Plugin.Log.Info("Multiplayer Ready !");
        }

        public void startSong()
        {
            if (ScoreController != null || SongController != null)
                return; // startSong already executed for this song

            ScoreController = Resources.FindObjectsOfTypeAll<ScoreController>().FirstOrDefault();
            if (ScoreController == null)
                Plugin.Log.Error("Couldn't find ScoreController object");

            SongController = Resources.FindObjectsOfTypeAll<SongController>().FirstOrDefault();
            if (SongController == null)
                Plugin.Log.Error("Couldn't find SongController object");

            setGameStatus(false);
            Plugin.Log.Info("Pausing Game");

            sendData("start");
            // send ready state to server
            sendData("ready");
            Plugin.Log.Info("Client ready [c]");
        }

        public Vector3 getLatestPosition()
        {
            return latestPosition;
        }
        public Quaternion getLatestRotation()
        {
            return latestRotation;
        }
        public void sendCoords(Vector3 pos, Quaternion rot)
        {
            string data = clientIdentifier.ToString() + ";" + pos.x + ";" + pos.y + ";" + pos.z + ";" + rot.x + ";" + rot.y + ";" + rot.z + ";" + rot.w;
            sendData(data);
        }

        public void stop()
        {
            client_connection.Disconnect("Plugin Exit");
        }

        private long getTimestamp()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
        }

        private void setGameStatus(bool enabled)
        {
            if (enabled)
            {
                ScoreController.enabled = true;
                SongController.StartSong();
            }
            else
            {
                ScoreController.enabled = false;
                SongController.PauseSong();
            }
        }

        private void parseMessage(string message)
        {
            string[] identifierData = message.Split(',');
            if (message == "ping")
            {
                // reply to server ping
                sendData("pong");
                Plugin.Log.Info("Sending pong");
            }
            else if(message == "start")
            {
                // start the client game
                setGameStatus(true);
                Plugin.Log.Info("Client game started");
            }
            else if(identifierData[0] == "identifier")
            {
                clientIdentifier = Int16.Parse(identifierData[1]);
            }
            else
            {
                // set new coords for remote saber
                string[] data = message.Split(';');
                latestPosition = new Vector3(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]));
                latestRotation = new Quaternion(float.Parse(data[3]), float.Parse(data[4]), float.Parse(data[5]), float.Parse(data[6]));
            }
        }

        public void checkMessages()
        {
            NetIncomingMessage message;
            while ((message = client_connection.ReadMessage()) != null)
            {
                if (message.MessageType == NetIncomingMessageType.Data)
                    parseMessage(message.ReadString());
                else
                    Plugin.Log.Debug(message.MessageType.ToString() + ":" + message.ReadString());
            }
        }

        private void sendData(string data)
        {
            NetOutgoingMessage msg = client_connection.CreateMessage();
            msg.Write(data);
            client_connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }
    }
}
