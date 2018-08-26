using Server_Structure.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Server_Structure
{
    public class Client
    {
        public static IPAddress GetlocalIP()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address;
                }
            }
            catch (SocketException e)
            {
                Server.WriteLine("You are not connected to the Internet.");
                return IPAddress.Loopback;
            }
        }
        public ClientContext context = new ClientContext();

        #region Gameplay Events
        public delegate void OnLevelChangedHandler(byte[] NewData);
        /// <summary>
        /// Raised when level files are received.
        /// </summary>
        public event OnLevelChangedHandler OnLevelChanged;
        public delegate void OnClientStateChanged(int ID, string message = "NO MESSAGE");
        /// <summary>
        /// Raised when the client has been kicked.
        /// </summary>
        public event OnClientStateChanged RemovedFromMatch;
        public event OnClientStateChanged HostStartedGame;
        public event OnClientStateChanged DisconnectedFromGame;
        public delegate void OnTextMessageReceivedHandler(string sender, string message);
        /// <summary>
        /// Called when the client receives a message from a remote connection.
        /// </summary>
        public event OnTextMessageReceivedHandler OnMessageReceived;
        public delegate void PlayerCreatedHandler(int ID);
        /// <summary>
        /// Raised when the client needs to create a new player character
        /// </summary>
        public event PlayerCreatedHandler OnPlayerCreated;
        public delegate void PlayerNavigateHandler(int ID, int DestinationX, int DestinationY);
        /// <summary>
        /// Raised when the player wants to navigate anywhere.
        /// </summary>
        public event PlayerNavigateHandler OnNavigation;
        public delegate void PlayerInteractionHandler(int ID, int OBJID);
        /// <summary>
        /// Raised when the player wants to interact with anything.
        /// </summary>
        public event PlayerInteractionHandler OnInteraction;
        public delegate void PersonSeatedHandler(int SenderID, int PersonID, int ObjectID, int Quadrant);
        /// <summary>
        /// Raised when the player wants to navigate anywhere.
        /// </summary>
        public event PersonSeatedHandler OnPersonSeated;
        #endregion

        /// <summary>
        /// Starts the client specialized for the game.
        /// </summary>
        /// <param name="ConnectTo"></param>
        public void StartGameClient(IPAddress ConnectTo)
        {
            Connect:
            Server.WriteLine("Trying connection", true);
            try
            {
                context.Client = new TcpClient();
                context.Client.Connect(new IPEndPoint(ConnectTo, 37563));
            }
            catch (SocketException e)
            {                
                Server.WriteLine(e.Message + Environment.NewLine + "Verify the IP: " + ConnectTo + " and make sure the host has portforwarded port 37563");
                DisconnectedFromGame?.Invoke(context.ID, e.Message);
            }
            if (context.Client.Connected)
                Server.WriteLine("Connected to: " + ConnectTo.ToString(), true);
            else
                return;
            try
            {
                PrepareForNextMessage();
            }
            catch (Exception e)
            {
                Server.WriteLine(e.Message, true);
#if DEBUG
                throw e;
#endif
            }
            Console.ReadLine();
        }

        public void RequestLevelChange(byte[] LevelData)
        {
            SendPacketToServer(DSPacket.Format(Commands.Command.LEVEL_NEWLEVEL, LevelData));
        }

        public void ReceiveLevel(byte[] buffer)
        {           
            if (OnLevelChanged != null)
                OnLevelChanged.Invoke(buffer);
        }

        private DSPacket BasePacket;
        private bool _waitingForCompletePacket;

        void PrepareForNextMessage()
        {
            context.Stream = context.Client.GetStream();
            context.Buffer = new byte[context.Client.ReceiveBufferSize];
            context.Message.Dispose();
            context.Message = new MemoryStream();
            context.Stream.BeginRead(context.Buffer, 0, context.Buffer.Length, OnMessageRecieved, null);
        }

        void ProcessMessage(int MessageBufferSize)
        {
            byte[] buffer = new byte[MessageBufferSize];
            context.Message.Position = 0;
            context.Message.Read(buffer, 0, MessageBufferSize);

            string message = "NO MESSAGE FORMATTED";
            var packets = DSPacket.Unfold(buffer);
            if (packets is null)
                return;
            var completed = new List<DSPacket>();
            foreach (var packet in packets)
            {
                if (!packet.HasEnd && !_waitingForCompletePacket)
                {
                    BasePacket = packet;
                    _waitingForCompletePacket = true;
                    continue;
                }
                if (!packet.HasBegin && _waitingForCompletePacket)
                {
                    var Packet = DSPacket.Merge(packet, BasePacket);
                    if (Packet.HasEnd)
                    {
                        _waitingForCompletePacket = false;
                        Server.WriteLine("[Client] Completed Packet Receieved: " + packet.data.Length);
                        completed.Add(Packet);
                    }
                    else
                        BasePacket = Packet;
                }
                else
                    completed.Add(packet);
            }
            foreach (var Packet in completed)
                switch ((Command)Packet.ServerCommand)
                {
                    case Command.SERVER_SHUTDOWN: //Server shutting down
                        {
                            var r = Encoding.ASCII.GetString(Packet.data);
                            int.TryParse(r.Substring(0, r.IndexOf('|')), out int id);
                            r = r.Remove(0, r.IndexOf('|') + 1);
                            Server.WriteLine($"{id} disconnected from the server because: {r}");
                            DisconnectedFromGame?.Invoke(id, r);
                        }
                        break;
                    case Command.CLIENT_KICK: //Someone Kicked   
                        {
                            var r = Encoding.ASCII.GetString(Packet.data);
                            int.TryParse(r.Substring(0, r.IndexOf('|')), out int id);
                            r = r.Remove(0, r.IndexOf('|') + 1);
                            Server.WriteLine($"{id} has been kicked from the server because: {r}");
                            RemovedFromMatch?.Invoke(id, r);
                        }
                        break;
                    case Command.LEVEL_LEVELCHANGED: //Switch Level
                        Server.WriteLine("Server forcing level change. Size: " + Packet.data.Length);
                        ReceiveLevel(Packet.data);
                        break;
                    case Command.CLIENT_IDCHANGE: //ID changed
                        if (Packet.data.Length > 1)
                        {
                            Server.WriteLine("Command Formatting is incorrect: No <byte> in byte array.");
                            return;
                        }
                        context.ID = Packet.data[0];
                        break;
                    case Command.CLIENT_CREATEPLAYER: //Create player for ClientContext
                        CreatePlayer(Packet.data);
                        break;
                    case Command.PLAYER_NAVIGATE: //Player navigates to a position
                        PlayerNavigate(Packet.data);
                        break;
                    case Command.CLIENT_MESSAGE_ALL: //Textmessage
                        message = Encoding.ASCII.GetString(Packet.data);
                        string sender = message.Substring(0, message.IndexOf('|'));
                        message = message.Substring(message.IndexOf('|') + 1);
                        OnMessageReceived?.Invoke(sender, message);
                        Server.WriteLine($"{sender}: {message}");
                        break;
                    case Command.PLAYER_INTERACT: //Player interacts with an object
                        PlayerInteract(Packet.data);
                        break;
                    case Command.PERSON_SEAT:
                        PersonSeated(Packet.data);
                        break;
                    case Command.HOST_STARTGAME: //Host start game
                        HostStartedGame?.Invoke(context.ID);
                        break;
                }
            PrepareForNextMessage();
        }

        private void PersonSeated(byte[] data)
        {
            var SenderID = BitConverter.ToInt32(data, 0);
            var PersonID = BitConverter.ToInt32(data, 4);
            var ObjectID = BitConverter.ToInt32(data, 8);
            var Quadrant = BitConverter.ToInt32(data, 12);
            OnPersonSeated?.Invoke(SenderID, PersonID, ObjectID, Quadrant);
            Server.WriteLine($"Player: {SenderID}, Seats Person: {PersonID} at Table: {ObjectID}");
        }

        private void PlayerNavigate(byte[] data)
        {
            var ID = BitConverter.ToInt32(data, 0);
            var x = BitConverter.ToInt32(data, 4);
            var y = BitConverter.ToInt32(data, 8);
            OnNavigation?.Invoke(ID, x, y);
            Server.WriteLine($"PLAYER Navigation X:{x}, Y:{y}");
        }

        private void PlayerInteract(byte[] data)
        {
            var ID = BitConverter.ToInt32(data, 0);
            var ObjID = BitConverter.ToInt32(data, 4);
            OnInteraction?.Invoke(ID, ObjID);
            Server.WriteLine($"Player: {ID}, Interacts with Object: {ObjID}");
        }

        private void CreatePlayer(byte[] data)
        {
            var i = BitConverter.ToInt32(data, 0);
            OnPlayerCreated?.Invoke(i);
            Server.WriteLine($"Player created with ID: {i}");
        }

        void OnMessageRecieved(IAsyncResult ar)
        {
            try
            {
                int read = context.Stream.EndRead(ar);
                context.Message.Write(context.Buffer, 0, read);
                ProcessMessage(read);
            }
            catch (IOException e) { DisconnectedFromGame?.Invoke(context.ID, e.Message); }
        }

        public void SendPacketToServer(byte[] FormattedPacket)
        {
            context.Client.SendBufferSize = FormattedPacket.Length;
            NetworkStream nwStream = context.Client.GetStream();         
            nwStream.Write(FormattedPacket, 0, FormattedPacket.Length);
        }
    }
}
