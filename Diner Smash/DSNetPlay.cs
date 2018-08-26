using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Server_Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static Diner_Smash.UserInterface;

namespace Diner_Smash
{
    public class DSNetPlay
    {        
        public int ID
        {
            get => (Main.PlacementMode) ? -1 : MultiplayerClient.context.ID;
        }
        public Client MultiplayerClient;
        public DSNetPlay(int mode = 0)
        {
            MultiplayerMode = mode;
            Main.GlobalInput.UserInput += GlobalInput_UserInput;
        }

        private async void GlobalInput_UserInput(InputHelper.InputEventArgs e)
        {
            if (e.PressedKeys.Contains(Keys.Enter) && Ready && !_chatBoxOpened)
            {
                var str = await PromptForTextMessage(true);
                if (str != "<break>")
                    MultiplayerClient.SendPacketToServer(DSPacket.Format(3, Encoding.ASCII.GetBytes(str)));
            }
        }

        /// <summary>
        /// 0 = host
        /// 1 = join by IPAddress
        /// 2 = join LAN hosted game
        /// </summary>
        public int MultiplayerMode { get; set; }
        public ObservableCollection<string> ChatMessages = new ObservableCollection<string>();

        #region MultiplayerPrompts
        public async void PromptHostJoin()
        {
            var hostJoinPrompt = new StackPanel(Color.DarkOrange * .75f, false);
            hostJoinPrompt.SetCenterScreen();
            hostJoinPrompt.AddRange(InterfaceComponent.HorizontalLock.Center,
                new InterfaceComponent().CreateText(new InterfaceFont(12, InterfaceFont.Styles.Bold), "What would you like to do?", Color.White, new Point(10)),
                new InterfaceComponent().CreateButton("Host Server", Color.Orange * .5f, Color.White,
                    Color.MonoGameOrange * .5f, Color.MonoGameOrange, new Rectangle(new Point(10, 5), new Point(-1, 50))),
                new InterfaceComponent().CreateButton("Join Server", Color.Orange * .5f, Color.White,
                    Color.MonoGameOrange * .5f, Color.MonoGameOrange, new Rectangle(new Point(10, 5), new Point(-1, 50))),
                new InterfaceComponent().CreateButton("Create Level", Color.Orange * .5f, Color.White,
                    Color.MonoGameOrange * .5f, Color.MonoGameOrange, new Rectangle(new Point(10, 5), new Point(-1, 50))),
                new InterfaceComponent().CreateButton("Quit", Color.Orange * .5f, Color.White,
                    Color.MonoGameOrange * .5f, Color.MonoGameOrange, new Rectangle(new Point(10, 5), new Point(-1, 50))));
            (hostJoinPrompt.Components[1] as Button).OnClick += (Button sender) =>
            {
                MultiplayerMode = 0;
                hostJoinPrompt.CloseDialog();
                InitializeMultiplayer(Main.SourceLevel);               
            };
            (hostJoinPrompt.Components[2] as Button).OnClick += (Button sender) =>
            {
                MultiplayerMode = 1;
                Main.UnloadLevel();
                hostJoinPrompt.CloseDialog();
                InitializeMultiplayer();                
            };
            (hostJoinPrompt.Components[4] as Button).OnClick += (Button sender) =>
            {
                Environment.Exit(0);
            };
            (hostJoinPrompt.Components[3] as Button).OnClick += (Button sender) => { hostJoinPrompt.CloseDialog(); LevelCreatorButtonClick(sender); };
            await hostJoinPrompt.ShowAsDialog();
        }

        private void LevelCreatorButtonClick(Button sender)
        {
            Main.FLAG_NetPlayGameStarted = true;
            Ready = true;
            Main.PlacementMode = true;
            System.Windows.Forms.MessageBox.Show("Welcome to Creator Mode!" + Environment.NewLine
               + "Check the GitHub README for Creator controls.", "Creator Mode");            
        }

        bool _chatBoxOpened = false;
        private Task<string> PromptForTextMessage(bool ShowTextBox)
        {            
            return Task.Run(() => {
                if (_chatBoxOpened)
                    return "<break>";
                if (ShowTextBox)
                    _chatBoxOpened = true;
                else
                    _chatBoxOpened = false;
                var textPrompt = new StackPanel(Color.Black * .75f, false);
                var done = "";
                textPrompt.HLock = InterfaceComponent.HorizontalLock.Left;
                textPrompt.VLock = InterfaceComponent.VerticalLock.Bottom;
                textPrompt.Exclusive = ShowTextBox;
                var updatingChatSummary = new InterfaceComponent().CreateText("", Color.White, new Point(10));
                int _prevChatMessages = -1;
                void UpdateChat()
                {
                    if (_prevChatMessages != ChatMessages.Count)
                    {
                        if (ChatMessages.Any())
                            updatingChatSummary.RenderText =
                                string.Join(Environment.NewLine, ChatMessages.ToArray());
                        else
                            updatingChatSummary.RenderText = "Nobody has said anything yet";
                        _prevChatMessages = ChatMessages.Count;
                        textPrompt.Reformat();
                    }
                }
                textPrompt.AddRange(InterfaceComponent.HorizontalLock.Left, updatingChatSummary,
                    ShowTextBox ? new InterfaceComponent().CreateTextBox("", Color.Black * .65f, Color.White,
                        Color.Black * .5f, Color.MidnightBlue * .5f, new Rectangle(new Point(10, 5), new Point(-1, 35))) : null);
                if (ShowTextBox)
                {
                    (textPrompt.Components[1] as TextBox).IsActive = true;
                    (textPrompt.Components[1] as TextBox).Accepted += (object sender) =>
                    {
                        done = ((TextBox)sender).RenderText;
                        if (string.IsNullOrEmpty(done))
                            done = "<break>";
                    };
                }
                textPrompt.AddToParent(Main.UILayer);
                var timer = new Timer(5000);
                timer.Elapsed += (object sender, ElapsedEventArgs e) => { done = "<break>"; timer.Dispose(); };
                if (!ShowTextBox)
                    timer.Start();
                while (string.IsNullOrEmpty(done)) { UpdateChat(); }
                textPrompt.RemoveFromParent();
                _chatBoxOpened = false;
                return done;
            });
        }

        private Task<IPAddress> PromptForIP()
        {
            return Task.Run(() =>
            {
                var IPsubmit = "";
                var hostJoinPrompt = new StackPanel(Color.RoyalBlue * .5f, false);
                hostJoinPrompt.CreateDialog("Enter the Host's IP Address", InterfaceComponent.HorizontalLock.Center, true,
                        new InterfaceComponent().CreateTextBox("", Color.Blue * .5f, Color.White,
                            Color.DeepSkyBlue * .75f, Color.DeepSkyBlue, new Rectangle(new Point(20, 10), new Point(300, 35))));                        
                void Event(object sender)
                {
                    IPsubmit = ((hostJoinPrompt.Components[1] as TextBox).RenderText);
                }                
                (hostJoinPrompt.Components[1] as TextBox).Accepted += Event;
                onError:
                if (hostJoinPrompt.ShowAsDialog().Result.Value)
                    Event(null);
                else
                    return IPAddress.None;
                var IP = IPAddress.None;
                try
                {
                    if (IPsubmit == "localhost")
                        IP = IPAddress.Loopback;
                    else
                        IP = IPAddress.Parse(IPsubmit);
                }
                catch (FormatException) { IPsubmit = ""; goto onError; }
                return IP;
            });
        }
        #endregion

        public bool Ready = false;

        /// <summary>
        /// Using the MultiplayerMode setting, sets up the game for multiplayer.
        /// </summary>
        public async void InitializeMultiplayer(LevelSave Level = null)
        {
            var JoinIP = IPAddress.Parse("127.0.0.1");
            switch (MultiplayerMode)
            {
                case 0: //host
                    CreateServer();
                    break;
                case 1:
                    if (!Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                        JoinIP = await PromptForIP();
                    if (JoinIP == IPAddress.None)
                    {
                        PromptHostJoin();
                        return;
                    }
                    else
                    {                        
                        break;
                    }
            }
            MultiplayerClient = new Client();
            #region Subscribe to events
            ChatMessages.CollectionChanged += (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => PromptForTextMessage(false);
            MultiplayerClient.OnLevelChanged += MultiplayerClient_OnLevelChanged;
            MultiplayerClient.RemovedFromMatch += MultiplayerClient_RemovedFromMatch;
            MultiplayerClient.HostStartedGame += MultiplayerClient_HostStartedGame;
            MultiplayerClient.DisconnectedFromGame += MultiplayerClient_DisconnectedFromGame;
            MultiplayerClient.OnPlayerCreated += MultiplayerClient_OnPlayerCreated;
            MultiplayerClient.OnNavigation += MultiplayerClient_OnNavigation;
            MultiplayerClient.OnInteraction += MultiplayerClient_OnInteraction;
            MultiplayerClient.OnPersonSeated += MultiplayerClient_OnPersonSeated;
            MultiplayerClient.OnMessageReceived += (string sender, string message) => ChatMessages.Add($"{sender}: {message}");
            #endregion
            MultiplayerClient.StartGameClient(JoinIP);
            if (MultiplayerMode == 0 && Level.Source != null)
                MultiplayerClient.RequestLevelChange(Level.Serialize());
            Ready = true;
            Main.UILayer.ShowNotification("Waiting for Host to Start Game...", Color.Red * .5f, Color.White);
            return;
        }

        /// <summary>
        /// Launches the server program
        /// </summary>
        public static void CreateServer()
        {
            var p = new ProcessStartInfo();
            p.FileName = Server.GetFileName;
            p.Arguments = "server";
            Process.Start(p);
        }

        private void MultiplayerClient_DisconnectedFromGame(int ID, string message)
        {
            if (ID == this.ID)
            {
                MultiplayerClient.DisconnectedFromGame -= MultiplayerClient_DisconnectedFromGame;
                System.Windows.Forms.MessageBox.Show(message, "Disconnected");
                Ready = false;
                Main.UpdateLevel(null);
            }
            else
                ChatMessages.Add($"Player: {ID} has disconnected");
        }        

        private void MultiplayerClient_HostStartedGame(int ID, string message)
        {
            Main.FLAG_NetPlayGameStarted = true;
            Main.UILayer.ShowNotification("The Host has Started The Game!", Color.Green * .5f, Color.White, TimeSpan.FromSeconds(5));
        }

        private void MultiplayerClient_OnPersonSeated(int SenderID, int PersonID, int ObjectID, int Quadrant)
        {
            var person = Main.Objects.OfType<Person>().Where(x => x.ID == PersonID).First();
            (Main.Objects.Find(x => x.ID == ObjectID && x is Table) as Table)?.Seat(Quadrant, person);
        }

        private void MultiplayerClient_OnInteraction(int ID, int OBJID)
        {
            var player = Main.Objects.OfType<Player>().Where(x => x.OwnerID == ID).First();
            new Task(() =>
                Main.Objects.Find(x => x.ID == OBJID).Interact(player, true)).Start();
                
        }

        private void MultiplayerClient_OnNavigation(int ID, int DestinationX, int DestinationY)
        {
            new Task(() =>
                (Main.Objects.Find(x => x is Player && (x as Player).OwnerID == ID) as Player).
                WalkToPoint(new Point(DestinationX, DestinationY))).Start();
        }

        /// <summary>
        /// Player connected
        /// </summary>
        /// <param name="ID"></param>
        private void MultiplayerClient_OnPlayerCreated(int ID)
        {
            ChatMessages.Add($"{(ID == this.ID ? "You have" : $"Player: {ID} has")} joined the game.");
            var p = new Player("multiplayerCharacter", ID);
            p.Location = new Vector2(100, 100);
            p.Load(Main.Manager);
            Main.Objects.Add(p);

        }

        private void MultiplayerClient_RemovedFromMatch(int ID, string message)
        {
            if (ID == MultiplayerClient.context.ID)
            {
                System.Windows.Forms.MessageBox.Show("The host kicked you because: " + message, "Kicked");
                Ready = false;
                Main.UpdateLevel(null);
            }
            else
                ChatMessages.Add($"Player: {ID} was removed from the game because: {message}");
        }

        private void MultiplayerClient_OnLevelChanged(byte[] NewData)
        {
            var l = LevelSave.DeserializeFromServer(NewData);
            Main.UpdateLevel(l);
        }

        public void NavigationRequest(Player sender, Point destination)
        {
            if (sender.OwnerID == MultiplayerClient.context.ID)
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(sender.OwnerID));
                bytes.AddRange(BitConverter.GetBytes(destination.X));
                bytes.AddRange(BitConverter.GetBytes(destination.Y));
                var b = DSPacket.Format(6, bytes.ToArray());
                MultiplayerClient.SendPacketToServer(b);
            }
        }
        public void PersonSeatRequest(Player sender, Person Person, int OBJID, int Quadrant)
        {
            if (sender.OwnerID == MultiplayerClient.context.ID)
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(sender.OwnerID));
                bytes.AddRange(BitConverter.GetBytes(Person.ID));
                bytes.AddRange(BitConverter.GetBytes(OBJID));
                bytes.AddRange(BitConverter.GetBytes(Quadrant));
                var b = DSPacket.Format(8, bytes.ToArray());
                MultiplayerClient.SendPacketToServer(b);
            }
        }
        public void InteractionRequest(Player sender, int OBJID)
        {
            if (sender.OwnerID == MultiplayerClient.context.ID)
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(sender.OwnerID));
                bytes.AddRange(BitConverter.GetBytes(OBJID));
                var b = DSPacket.Format(7, bytes.ToArray());
                MultiplayerClient.SendPacketToServer(b);
            }
        }
    }
}
