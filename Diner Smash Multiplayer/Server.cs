﻿using Server_Structure.Commands;
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
        bool _disposed;
        /// <summary>
        /// Acts as a flag that shows whether or not this context is usable.
        /// </summary>
        public bool Disposed
        {
            get => _disposed || !Client.Connected;
            private set => _disposed = value;
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

        public static byte[] Format(Command ServerCommand, byte[] data)
        {
            byte[] b = new byte[(data.Length + 7)];
            b[0] = 30;
            b[1] = (byte)ServerCommand;
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
        public static IPAddress HostingAddress { get => Client.GetlocalIP(); }
        public const int Port = 37563;
        public const string ServerExecutableName = "Diner Server.exe";
        public static string GetFileName
        {
            get => Path.Combine(Environment.CurrentDirectory, ServerExecutableName);
        }
        public static List<string> ConsoleLines = new List<string>();        
        static bool? ISCONSOLE = null;
        public static byte[] SERVER_LevelFile
        {
            get
            {
                return _levelFile;
            }
            set
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

        public static bool GameInProgress = true;
        /// <summary>
        /// Used to sync up newly joined clients.
        /// </summary>
        public static List<byte[]> PacketsBroadcasted = new List<byte[]>();

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

        /// <summary>
        /// Launches the server for connections...
        /// </summary>
        public static void StartServer(IPAddress OperatorIPAddress, bool IsConsole)
        {
            if (IsConsole)
                Console.Clear();
            ISCONSOLE = IsConsole;
            Connections.CollectionChanged += OnClientAmountChanged;
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
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
            WriteLine("Waiting...");
            ServerSocket = listener.Server;
            listener.Server.NoDelay = true;            
            listener.BeginAcceptTcpClient(OnClientAccepted, listener);
            loopback:
            if (RunTextCommand(Console.ReadLine()))
                goto loopback;
            if (FLAG_Quit)
            {
                foreach (var c in Connections)
                    CloseConnection(c, "", Command.CLIENT_KICK);
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
            Console.Title = $"Diner Smash | HOSTING: {HostingAddress} / PORT: {Port} | {Connections.Count} Connections";
        }

        static bool FLAG_WAITKICK;
        /// <summary>
        /// Runs a command
        /// </summary>
        /// <param name="command">The command to run</param>
        /// <param name="PreParameters">leave these blank they are situational</param>
        /// <returns>if false, the loopback is ignored closing the server.</returns>
        static bool RunTextCommand(string command, params string[] PreParameters)
        { 
            asCommand:
            if (FLAG_WAITKICK)
            {
                FLAG_WAITKICK = false;
                if (command != "kick")
                    goto asCommand;
                var id = "-1";
                if (PreParameters.Length > 0)
                    id = PreParameters[0];
                int.TryParse(PreParameters.First(), out int i);
                if (Connections.Where(x => x.ID == i).Any())
                {
                    var r = "<no reason given>";
                    if (PreParameters.Length == 2)
                        r = PreParameters[1];
                    CloseConnection(Connections.Where(x => x.ID == i).First(), r, Command.CLIENT_KICK);
                    return true;
                }
                else
                    WriteLine("No clients have that ID... Nobody was kicked");
                return true;
            }
            if (FLAG_WAITSAY)
            {
                FLAG_WAITSAY = false;
                if (command != "say")
                    goto asCommand;
                BroadcastASCIIMessage("SERVER", PreParameters.First(), BroadcastAudience.All);
                return true;
            }
            try
            {
                var currentParam = "";
                var parameters = new List<string>();
                if (command.Contains(' '))
                {
                    foreach (var c in command.Substring(command.IndexOf(' ') + 1))
                    {
                        if (c == ';')
                        {
                            parameters.Add(currentParam);
                            currentParam = "";
                            continue;
                        }
                        currentParam += c;
                    }
                    if (currentParam.Length > 0)
                        parameters.Add(currentParam);
                    command = command.Remove(command.IndexOf(' '));
                }
                command = command.TrimStart('/');
                switch (command)
                {
                    case "dump":
                        DUMP();
                        return true;
                    case "quit":
                        FLAG_Quit = true;
                        return false;
                    case "kick":
                        FLAG_WAITKICK = true;
                        if (parameters.Any())
                            RunTextCommand(command, parameters.ToArray());
                        else
                            WriteLine("Kick who? Waiting for context ID...");                        
                        return true;
                    case "ready":
                        HostStartedGameCommand.BroadcastGameStart();
                        return true;
                    case "say":
                        FLAG_WAITSAY = true;
                        if (parameters.Any())
                            RunTextCommand(command, parameters.ToArray());
                        else
                            WriteLine("Broadcast what? Waiting for ASCII message...");
                        return true;
                    case "info":

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
                    switch ((Command)Packet.ServerCommand)
                    {
                        case Command.CLIENT_KICK:
                            CloseConnection(context, "<no reason>", Command.CLIENT_KICK);
                            break;
                        case Command.CLIENT_MESSAGE_ALL:
                            BroadcastASCIIMessage(context.Gamertag, Encoding.ASCII.GetString(Packet.data), BroadcastAudience.All);
                            break;
                        case Command.LEVEL_NEWLEVEL:
                            WriteLine("Client has requested to update server level. Size: " + Packet.data.Length);
                            LevelChangedCommand.UpdateLevel(context, Packet.data);
                            break;
                        case Command.PLAYER_NAVIGATE:
                            PlayerNavigationCommand.PlayerNavigated(context, Packet.data);
                            break;
                        case Command.PLAYER_INTERACT:
                            PlayerInteractionCommand.PlayerInteracted(context, Packet.data);
                            break;
                        case Command.PERSON_SEAT:
                            PersonSeatedCommand.PersonSeated(context, Packet.data);                            
                            break;
                        case Command.HOST_STARTGAME:
                            HostStartedGameCommand.BroadcastGameStart();
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
            catch (InvalidOperationException e) //Most likely force close
            {
                CloseConnection(context, "They closed the game", 0);
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Run these tasks for everyone
        /// </summary>
        /// <param name="Joined"></param>
        static async void RunWhenPlayerJoins(ClientContext Joined)
        {
            Joined.Operator = VerifyIPasOperator(Joined);
            Connections.Add(Joined);
            SyncContextID(Joined, (byte)(Connections.Count));
            foreach(var c in Connections.Where(x => x != Joined))
            {
                CreatePlayerCommand.CreatePlayer(c, Joined.ID);
            }            
            LevelChangedCommand.SendLevel(Joined);
            CreatePlayerCommand.BroadcastCreateAllPlayers(Joined);
            await SyncClient(Joined);
        }

        /// <summary>
        /// Used to blast a client who joined in progress to sync them up to where the game is now.
        /// </summary>
        static Task SyncClient(ClientContext Send)
        {
            return Task.Run(() =>
            {
                if (PacketsBroadcasted.Count == 0)
                    return;
                WriteLine($"Syncing Client: {Send.ID}");
                foreach (var p in PacketsBroadcasted)
                    SendPacketToClient(Send, p);
                WriteLine($"Syncing Complete... {PacketsBroadcasted.Count} packets sent");
            });
        }

        /// <summary>
        /// Change the ID of a client also making sure the client updates it's stored ID.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="newID"></param>
        static void SyncContextID(ClientContext context, byte newID)
        {
            var old_id = context.IP;
            SendPacketToClient(context, DSPacket.Format(Command.CLIENT_IDCHANGE, new byte[1] { newID }));
            context.ID = newID;
            WriteLine(old_id + " has ID: " + context.ID);
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
        /// <param name="mode">1: Client was removed; 0: Client disconnected</param>
        static void CloseConnection(ClientContext context, string reason, Command mode)
        {
            try
            {
                var r = Encoding.ASCII.GetBytes($"{context.ID}|{reason}");
                BroadcastPacket(DSPacket.Format(mode, r), BroadcastAudience.All);
                context.Dispose();
            }
            catch { }            
            WriteLine($"Client: {context.ID} {(mode == Command.CLIENT_KICK ? "was kicked" : "disconnected")} because: " + reason);
            Connections.Remove(context);
        }

        static void OnClientRead(IAsyncResult ar)
        {
            ClientContext context = ar.AsyncState as ClientContext;
            try
            {
                if (context == null)
                    return;
                int read = context.Stream.EndRead(ar);
                if (read == 0)
                    return;
                context.Message.Write(context.Buffer, 0, read);
                OnMessageReceived(context, read);
            }
            catch (ObjectDisposedException e)
            {
                CloseConnection(context, e.Message, 0);
            }
            catch (IOException e)
            {
                CloseConnection(context, e.Message, 0);
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
                if (context.Disposed)
                    return;
                context.Stream = context.Client.GetStream();
                context.Buffer = new byte[context.Client.ReceiveBufferSize];
                context.Message = new MemoryStream();
                context.Stream.BeginRead(context.Buffer, 0, context.Buffer.Length, OnClientRead, context);
            }
            catch (InvalidOperationException e) //Force Close
            {
                CloseConnection(context, e.Message, 0);
            }
        }

        public enum BroadcastAudience
        {
            All,
        }

        public static void BroadcastASCIIMessage(string sender, string message, BroadcastAudience audience = BroadcastAudience.All)
        {
            int messageattempts = 0;
            if (audience == BroadcastAudience.All)
                foreach (var client in Connections)
                {
                    SendPacketToClient(client, DSPacket.Format(Command.CLIENT_MESSAGE_ALL, Encoding.ASCII.GetBytes($"{sender}|{message}")));
                    messageattempts++;
                }
            WriteLine($"[Messenger] {sender}: {message}");
        }

        public static void BroadcastPacket(byte[] FormattedPacket, BroadcastAudience audience = BroadcastAudience.All)
        {
            foreach (var c in Connections)
                SendPacketToClient(c, FormattedPacket);
            PacketsBroadcasted.Add(FormattedPacket);
        }

        public static void SendPacketToClient(ClientContext context, byte[] FormattedPacket)
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

