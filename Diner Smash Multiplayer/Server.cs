using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server_Structure
{
    public class ClientContext
    {
        public ClientContext()
        {
            
        }

        public TcpClient Client;
        public NetworkStream Stream;
        public byte[] Buffer = new byte[4];
        public MemoryStream Message = new MemoryStream();

        public string Gamertag { get; set; } = "Anonymous";

        /// <summary>
        /// The id used to identify the context.
        /// </summary>
        public int ID;

        /// <summary>
        /// Provides the IP of the client at join time.
        /// </summary>
        public IPAddress IP;

        public IPAddress GetIP()
        {
            return ((IPEndPoint)Client.Client.RemoteEndPoint).Address;
        }
        public bool Operator;

        /// <summary>
        /// Acts as a flag that shows whether or not this context is usable.
        /// </summary>
        public bool Disposed
        {
            get;
            private set;
        }

        public void Dispose()
        {
            Client.Close();
            Stream.Dispose();
            Message.Dispose();
            Disposed = true;
        }
    }

    public struct DSPacket
    {
        public const int PACKET_DataIndex = 6;
        public byte ServerCommand { get; private set; }
        public int EXPECTED_SIZE;
        public byte[] data;
        public int GetSize
        {
            get => data.Length + 7;
        }

        /// <summary>
        /// Packet ends with ascii control character 27, if not found when Unfold is called, this is false.
        /// </summary>
        public bool HasEnd
        {
            get; internal set;
        }
        /// <summary>
        /// Packet starts with ascii control character 30, if not found when Unfold is called, this is false.
        /// </summary>
        public bool HasBegin
        {
            get; internal set;
        }

        public static byte[] Format(byte ServerCommand, byte[] data)
        {
            byte[] b = new byte[(data.Length + 7)];
            b[0] = 30;
            b[1] = ServerCommand;
            var size = BitConverter.GetBytes(b.Length);
            b[2] = size[0];
            b[3] = size[1];
            b[4] = size[2];
            b[5] = size[3];
            Array.Copy(data, 0, b, PACKET_DataIndex, data.Length);
            b[b.Length - 1] = 27; //ASCII ESC character
            return b;
        }

        public static List<DSPacket> Unfold(byte[] raw)
        {
            if (raw.Length == 0)
                return null;
            var p = new DSPacket();
            if (raw[0] == 30)
                p.HasBegin = true;
            else
                p.HasBegin = false;
            if (p.HasBegin)
            {
                p.ServerCommand = raw[1];
                p.EXPECTED_SIZE = BitConverter.ToInt32(raw, 2);
                p.data = new byte[p.EXPECTED_SIZE - 7];
                Array.Copy(raw.Skip(PACKET_DataIndex).ToArray(), p.data, p.data.Length);               
            }
            else
                p.data = raw;
            var result = new List<DSPacket>();            
            if (raw[p.GetSize - 1] == 27)            
                p.HasEnd = true;                       
            else
                p.HasEnd = false;
            result.Add(p);
            if (raw.Length > p.GetSize)
            {
                result.AddRange(Unfold(raw.Skip(p.GetSize).ToArray()));
            }
            return result;
        }

        /// <summary>
        /// Merges the two packets if requirements are met.
        /// </summary>
        /// <param name="Source">No requirements</param>
        /// <param name="Destination">HasBegin must be false.</param>
        /// <returns></returns>
        public static DSPacket Merge(DSPacket Source, DSPacket Destination)
        {
            var error = "NO ERROR";
            if (!Destination.HasBegin)
            {
                error = ("[Server.PacketFormatter] Destination packet is incomplete. (HasBegin is false)");
                throw new Exception(error);
            }
            if (Source.HasBegin)
            {
                error = ("[Server.PacketFormatter] Source packet is the beginning of a formatted packet. (HasBegin is true)");
                throw new Exception(error);
            }
            var buffer = new byte[Destination.data.Length + Source.data.Length];
            Destination.data.CopyTo(buffer, 0);
            Array.Copy(Source.data, 0, buffer, Destination.data.Length, Source.data.Length);
            Destination.data = buffer;
            Destination.HasEnd = Source.HasEnd;
            return Destination;
        }
    }

    public class Server {
        public const string ServerExecutableName = "Diner Server.exe";
        public static string GetFileName
        {
            get => Path.Combine(Environment.CurrentDirectory, ServerExecutableName);
        }
        public static List<string> ConsoleLines = new List<string>();
        public static void WriteLine(string message, bool IsConsole = true)
        {
            if (ISCONSOLE.HasValue)
                IsConsole = ISCONSOLE.Value;
            var msg = DateTime.Now + ": " + message;
            Debug.WriteLine(msg);
            if (IsConsole)
                Console.WriteLine(msg);
            ConsoleLines.Add(msg);
        }
        public static void DUMP(string path = "")
        {
            if (path == "")
                path = Path.Combine(Environment.CurrentDirectory, "log.txt");
            using (var s = new StreamWriter(path, false, Encoding.UTF8))
            {
                s.WriteLine($"---LOG CREATED AT: {DateTime.Now}---");
                s.Write(string.Join(s.NewLine, ConsoleLines.ToArray()));
                s.Flush();
                s.Close();
            }
            WriteLine("Wrote buffer to log.txt");
        }
        static bool? ISCONSOLE = null;
        public static byte[] SERVER_LevelFile
        {
            get
            {
                return _levelFile;
            }

            private set
            {
                _levelFile = value;
                WriteLine($"Level file was changed! Size: {_levelFile.Length}");
            }
        }
        public static IPAddress Operator
        {
            get; private set;
        }
        static Socket ServerSocket;
        public static ObservableCollection<ClientContext> Connections = new ObservableCollection<ClientContext>();
        private static byte[] _levelFile;
        static bool FLAG_Quit;

        /// <summary>
        /// Launches the server for connections...
        /// </summary>
        public static void StartServer(IPAddress OperatorIPAddress, bool IsConsole)
        {
            if (IsConsole)
                Console.Clear();
            ISCONSOLE = IsConsole;
            Connections.CollectionChanged += OnClientAmountChanged;
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, 37563));
            try
            {
                listener.Start();
            }
            catch (SocketException e)
            {
                WriteLine(e.ToString());
#if DEBUG
                throw e;
#endif
            }
            Operator = OperatorIPAddress;            
            WriteLine("Waiting for Connections...");
            ServerSocket = listener.Server;
            listener.Server.NoDelay = true;            
            listener.BeginAcceptTcpClient(OnClientAccepted, listener);
            loopback:
            if (RunTextCommand(Console.ReadLine()))
                goto loopback;
            if (FLAG_Quit)
            {
                foreach (var c in Connections)
                    CloseConnection(c, "", 1);
                listener.Stop();
            }
        }

        public static bool VerifyIPasOperator(ClientContext context)
        {
            if (IPAddress.IsLoopback(context.IP))
                return true;
            return context.IP.Equals(Operator);
        }

        private static void OnClientAmountChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                WriteLine(Connections.Last().IP + $": Connected, Is Operator: {Connections.Last().Operator}");
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                WriteLine((e.OldItems[0] as ClientContext).IP + ": Disconnected");
            Console.Title = "Diner Smash Server: " + Connections.Count + " Connections";
        }

        static bool FLAG_WAITKICK;
        /// <summary>
        /// Runs a command
        /// </summary>
        /// <param name="command"></param>
        /// <returns>if false, the loopback is ignored closing the server.</returns>
        static bool RunTextCommand(string command)
        {
            if (FLAG_WAITKICK)
            {
                FLAG_WAITKICK = false;
                int.TryParse(command, out int i);
                if (Connections.Where(x => x.ID == i).Any())
                {
                    CloseConnection(Connections.Where(x => x.ID == i).First(), "<no reason>");
                    return true;
                }
                return true;
            }
            if (FLAG_WAITSAY)
            {
                FLAG_WAITSAY = false;
                BroadcastMessage("SERVER", command, BroadcastAudience.All);
                return true;
            }
            try
            {
                switch (command.TrimStart('/'))
                {
                    case "dump":
                        DUMP();
                        return true;
                    case "quit":
                        FLAG_Quit = true;
                        return false;
                    case "kick":
                        WriteLine("Kick who? Waiting for context ID...");
                        FLAG_WAITKICK = true;
                        return true;
                    case "ready":
                        BroadcastGameStart();
                        return true;
                    case "say":
                        WriteLine("Broadcast what? Waiting for ASCII message...");
                        FLAG_WAITSAY = true;
                        return true;
                }
            }
            catch (InvalidOperationException e)
            {
                WriteLine("The command entered was not recognized.");
                return true;
            }
            return true;
        }

        private static void BroadcastGameStart()
        {
            var packet = DSPacket.Format(9, new byte[0]);
            foreach (var c in Connections)
                SendPacketToClient(c, packet);
        }

        #region DataPersistance
        /// <summary>
        /// Assigned when the packet is verified to be incomplete.
        /// </summary>
        static DSPacket BasePacket;
        static bool _waitingForCompletePacket = false;
        private static bool FLAG_WAITSAY;
        #endregion

        //[DebuggerStepThrough]
        static void VerifyMessageAsServerCommand(ClientContext context, byte[] buffer)
        {
            DSPacket LAST_DITCH_PACKET = new DSPacket();
            string message = "NO MESSAGE FORMATTED";
            try
            {
                var packets = DSPacket.Unfold(buffer);
                var completed = new List<DSPacket>();
                foreach (var packet in packets)
                {
                    LAST_DITCH_PACKET = packet;
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
                            WriteLine("Completed Packet Receieved: " + Packet.data.Length);
                            completed.Add(Packet);
                        }
                        else
                            BasePacket = Packet;
                    }
                    if (packet.HasBegin && packet.HasEnd)
                        completed.Add(packet);
                }
                foreach (var Packet in completed)
                    switch (Packet.ServerCommand)
                    {
                        case 1: //Kicks client
                            CloseConnection(context, "<no reason>");
                            break;
                        case 2: //Sends a text message to server
                            message = ASCIIEncoding.ASCII.GetString(Packet.data);
                            WriteLine("Client: " + context.ID + " whispered: " + message);
                            break;
                        case 3: //Sends a text message to all clients
                            BroadcastMessage(context.Gamertag, Encoding.ASCII.GetString(Packet.data), BroadcastAudience.All);
                            break;
                        case 4: //Updates the server level and forces clients to switch levels
                            WriteLine("Client has requested to update server level. Size: " + Packet.data.Length);
                            UpdateLevel(context, Packet.data);
                            break;
                        case 6: //Player Requesting Navigation
                            PlayerNavigated(context, Packet.data);
                            break;
                        case 7: //Player Requesting Navigation
                            PlayerInteracted(context, Packet.data);
                            break;
                        case 8: //Seated Person
                            PersonSeated(context, Packet.data);
                            break;
                        case 9: //Host start game
                            BroadcastGameStart();
                            break;
                    }               
            }
            catch (SerializationException e)
            {
                WriteLine("[SerializationException]: Attempting last-ditch packet receive");
                LAST_DITCH_PACKET.HasEnd = false;
                BasePacket = LAST_DITCH_PACKET;
                _waitingForCompletePacket = true;
            }
            catch (InvalidOperationException) //Most likely force close
            {
                CloseConnection(context, "error");
            }
            catch (Exception e)
            {
                WriteLine(e.Message);                
#if DEBUG
                throw e;
#endif
            }
        }

        /// <summary>
        /// Run these tasks for everyone
        /// </summary>
        /// <param name="Joined"></param>
        static void RunWhenPlayerJoins(ClientContext Joined)
        {
            foreach(var c in Connections.Where(x => x != Joined))
            {
                CreatePlayer(c, Joined.ID);
            }
            Joined.Operator = VerifyIPasOperator(Joined);
            Connections.Add(Joined);
            SyncContextID(Joined, (byte)(Connections.Count));
            SendLevel(Joined);
            BroadcastCreateAllPlayers(Joined);
        }

        /// <summary>
        /// Tells the client to create a player for the context ID.
        /// </summary>
        /// <param name="SendTo"></param>
        /// <param name="PlayerID"></param>
        static void CreatePlayer(ClientContext SendTo, int PlayerID)
        {
            var b = BitConverter.GetBytes(PlayerID);
            var send = DSPacket.Format(4, b);
            SendPacketToClient(SendTo, send);
            WriteLine($"Player created on context: {SendTo.ID}, for ID: {PlayerID}");
        }

        static void BroadcastCreateAllPlayers(ClientContext SendTo)
        {
            foreach (var id in Connections.Select(x => x.ID))
                CreatePlayer(SendTo, id);
        }

        private static void PlayerNavigated(ClientContext context, byte[] data)
        {
            var b = DSPacket.Format(5, data);
            foreach(var i in Connections)
            {               
                SendPacketToClient(i, b);
            }
            var x = BitConverter.ToInt32(data, 4);
            var y = BitConverter.ToInt32(data, 8);
            WriteLine($"Navigation Accepted X:{x}, Y:{y}");
        }

        private static void PlayerInteracted(ClientContext context, byte[] data)
        {
            var b = DSPacket.Format(7, data);
            foreach (var i in Connections)
            {
                SendPacketToClient(i, b);
            }
            var x = BitConverter.ToInt32(data, 0);
            var y = BitConverter.ToInt32(data, 4);
            WriteLine($"Player: {x}, Interacts with Object: {y}");
        }

        private static void PersonSeated(ClientContext context, byte[] data)
        {
            var b = DSPacket.Format(8, data);
            foreach (var i in Connections)
            {
                SendPacketToClient(i, b);
            }
            var SenderID = BitConverter.ToInt32(data, 0);
            var pID = BitConverter.ToInt32(data, 4);
            var table = BitConverter.ToInt32(data, 8);
            WriteLine($"Player: {SenderID}, Seats Person: {pID} at Table: {table}");
        }

        /// <summary>
        /// Called after command /S_LEVEL is called. Server waits for next message from client containing level data.
        /// </summary>
        /// <param name="context"></param>
        static bool UpdateLevel(ClientContext context, byte[] buffer)
        {
            if (!context.Operator)
            {
                WriteLine("Client: " + context.ID + " is not an operator and cannot change the server level.");
                return false;
            }
            SERVER_LevelFile = buffer;
            return true;
        }

        static void SyncContextID(ClientContext context, byte newID)
        {
            int old_id = context.ID;
            SendPacketToClient(context, DSPacket.Format(3, new byte[1] { newID }));
            context.ID = newID;
            WriteLine("Client: " + old_id + " is now: " + context.ID);
        }

        /// <summary>
        /// Sends the current level to client, forcing it to update it's level.
        /// </summary>
        /// <param name="context"></param>
        static void SendLevel(ClientContext context)
        {
            if (SERVER_LevelFile == null)
            {
                WriteLine("[SendLevel] Level is not present and therefore command was aborted.");
                return;
            }
            WriteLine($"Sending level data to context: {context.ID}");
            SendPacketToClient(context, DSPacket.Format(2, SERVER_LevelFile));
        }

        /// <summary>
        /// Processes the message into a string and determines if it is a server command.
        /// </summary>
        /// <param name="MessageLength"></param>
        static void OnMessageReceived(ClientContext context, int MessageLength)
        {
            byte[] buffer = new byte[MessageLength];
            context.Message.Position = 0;
            context.Message.Read(buffer, 0, buffer.Length);
            VerifyMessageAsServerCommand(context, buffer);
        }

        /// <summary>
        /// Closes the connection with the specified client context.
        /// </summary>
        /// <param name="context"></param>
        static void CloseConnection(ClientContext context, string reason, int mode = 0)
        {
            try
            {
                if (mode == 0)
                    SendPacketToClient(context, DSPacket.Format(1, Encoding.ASCII.GetBytes(reason)));
                else if (mode == 1)
                    SendPacketToClient(context, DSPacket.Format(0, new byte[0]));
            }
            catch { }
            context.Dispose();
            WriteLine("Client: " + context.ID + " was disconnected because: " + reason);   
            if (mode != 1)
                Connections.Remove(context);            
        }

        static void OnClientRead(IAsyncResult ar)
        {
            ClientContext context = ar.AsyncState as ClientContext;
            if (context == null)
                return;

            try
            {
                int read = context.Stream.EndRead(ar);
                if (read == 0)
                    return;
                context.Message.Write(context.Buffer, 0, read);                
                OnMessageReceived(context, read);
            }
            catch (System.Exception e)
            {
                WriteLine(e.Message);
#if DEBUG
                //throw e;
#endif
            }
            finally
            {
                if (!context.Disposed)
                    PrepareForNextMessage(context);
            }
        }        

        static void PrepareForNextMessage(ClientContext context)
        {
            try
            {
                context.Stream = context.Client.GetStream();
                context.Buffer = new byte[context.Client.ReceiveBufferSize];
                context.Message = new MemoryStream();
                context.Stream.BeginRead(context.Buffer, 0, context.Buffer.Length, OnClientRead, context);
            }
            catch (InvalidOperationException e)
            {
                CloseConnection(context, "error");
            }
        }

        public enum BroadcastAudience
        {
            All,
        }

        static void BroadcastMessage(string sender, string message, BroadcastAudience audience)
        {
            int messageattempts = 0;
            if (audience == BroadcastAudience.All)
                foreach (var client in Connections)
                {
                    SendPacketToClient(client, DSPacket.Format(6, Encoding.ASCII.GetBytes($"{sender}|{message}")));
                    messageattempts++;
                }
            WriteLine($"[Messenger] {sender}: {message}");
        }

        static void SendPacketToClient(ClientContext context, byte[] FormattedPacket)
        {
            NetworkStream nwStream = context.Client.GetStream();
            Debug.WriteLine("Sending Packet to Client: " + FormattedPacket.Length);
            ServerSocket.SendBufferSize = FormattedPacket.Length;
            nwStream.Write(FormattedPacket, 0, FormattedPacket.Length);
        }

        /// <summary>
        /// When a connection attempt is made it needs to be validated here.
        /// </summary>
        /// <param name="ar"></param>
        static void OnClientAccepted(IAsyncResult ar)
        {
            TcpListener listener = ar.AsyncState as TcpListener;            
            if (listener == null)
            {
                WriteLine("A remote connection attempt was null and has been rejected.");                
                return;
            }

            try
            {
                ClientContext context = new ClientContext();
                context.Client = listener.EndAcceptTcpClient(ar);
                context.IP = context.GetIP();
                RunWhenPlayerJoins(context);               
                PrepareForNextMessage(context);                
            }
            finally
            {
                listener.BeginAcceptTcpClient(OnClientAccepted, listener);
            }
        }        
    }
}

