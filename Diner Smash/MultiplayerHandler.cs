using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Server_Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Diner_Smash.UserInterface;

namespace Diner_Smash
{
    public class MultiplayerHandler
    {        
        public Client MultiplayerClient;
        public MultiplayerHandler(int mode = 0)
        {
            MultiplayerMode = mode;
        }

        /// <summary>
        /// 0 = host
        /// 1 = join by IPAddress
        /// 2 = join LAN hosted game
        /// </summary>
        public int MultiplayerMode { get; set; }

        StackPanel hostJoinPrompt;
        public void PromptUser()
        {
            hostJoinPrompt = new StackPanel(Color.DarkOrange * .75f, false);
            hostJoinPrompt.CenterScreen = true;
            hostJoinPrompt.Exclusive = true;
            hostJoinPrompt.AddRange(true,
                new InterfaceComponent().CreateText("What would you like to do?", Color.White, new Point(10)),
                new InterfaceComponent().CreateButton("Host Server", Color.Orange * .5f, Color.White,
                    Color.MonoGameOrange * .5f, Color.MonoGameOrange, new Rectangle(new Point(10, 5), new Point(200, 50))),
                new InterfaceComponent().CreateButton("Join Server", Color.Orange * .5f, Color.White,
                    Color.MonoGameOrange * .5f, Color.MonoGameOrange, new Rectangle(new Point(10, 5), new Point(200, 50))),
                new InterfaceComponent().CreateButton("Create Level", Color.Orange * .5f, Color.White,
                    Color.MonoGameOrange * .5f, Color.MonoGameOrange, new Rectangle(new Point(10, 5), new Point(200, 50))));
            (hostJoinPrompt.Components[1] as Button).OnClick += HostButtonClick;
            (hostJoinPrompt.Components[2] as Button).OnClick += JoinButtonClick;
            (hostJoinPrompt.Components[3] as Button).OnClick += LevelCreatorButtonClick;
            Main.UILayer.Components.Add(hostJoinPrompt);
        }

        private void LevelCreatorButtonClick(Button sender)
        {
            Main.FLAG_NetPlayGameStarted = true;
            Ready = true;
            System.Windows.Forms.MessageBox.Show("Welcome to Creator Mode!" + Environment.NewLine
               + "Check the GitHub README for Creator controls.", "Creator Mode");
            Main.UILayer.Components.Remove(hostJoinPrompt);
        }

        private void JoinButtonClick(Button sender)
        {
            MultiplayerMode = 1;
            Main.Objects.Clear();
            InitializeMultiplayer();
        }

        private void HostButtonClick(Button sender)
        {
            MultiplayerMode = 0;
            InitializeMultiplayer(Main.SourceLevel);
        }

        public bool Ready = false;

        /// <summary>
        /// Using the MultiplayerMode setting, sets up the game for multiplayer.
        /// </summary>
        public async void InitializeMultiplayer(LevelSave Level = null)
        {
            Main.UILayer.Components.Remove(hostJoinPrompt);
            var JoinIP = IPAddress.Parse("127.0.0.1");
            switch (MultiplayerMode)
            {
                case 0: //host
                    CreateServer();
                    break;
                case 1:           
                    if(!Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                        JoinIP = await PromptForIP();
                    break;
            }
            MultiplayerClient = new Client();
            MultiplayerClient.OnLevelChanged += MultiplayerClient_OnLevelChanged;
            MultiplayerClient.RemovedFromMatch += MultiplayerClient_RemovedFromMatch;
            MultiplayerClient.HostStartedGame += MultiplayerClient_HostStartedGame;
            MultiplayerClient.DisconnectedFromGame += MultiplayerClient_DisconnectedFromGame;
            MultiplayerClient.OnPlayerCreated += MultiplayerClient_OnPlayerCreated;
            MultiplayerClient.OnNavigation += MultiplayerClient_OnNavigation;
            MultiplayerClient.OnInteraction += MultiplayerClient_OnInteraction;
            MultiplayerClient.OnPersonSeated += MultiplayerClient_OnPersonSeated;
            MultiplayerClient.StartGameClient(JoinIP);
            if (MultiplayerMode == 0 && Level != null)
                MultiplayerClient.RequestLevelChange(Level.Serialize());
            Ready = true;
            Main.UILayer.ShowNotification("Waiting for Host to Start Game...", Color.Red * .5f, Color.White);
        }

        private void MultiplayerClient_DisconnectedFromGame(string message)
        {
            MultiplayerClient.DisconnectedFromGame -= MultiplayerClient_DisconnectedFromGame;
            System.Windows.Forms.MessageBox.Show(message, "Disconnected");
            Ready = false;
            Main.UpdateLevel(null);
        }

        private Task<IPAddress> PromptForIP()
        {
            return Task.Run(() =>
            {
                var IPsubmit = "";
                var hostJoinPrompt = new StackPanel(Color.RoyalBlue * .75f, false);
                hostJoinPrompt.CenterScreen = true;
                hostJoinPrompt.Exclusive = true;
                hostJoinPrompt.AddRange(true,
                    new InterfaceComponent().CreateText("Enter the Host's IP Address", Color.White, new Point(10)),
                    new InterfaceComponent().CreateTextBox("", Color.Blue * .5f, Color.White,
                        Color.DeepSkyBlue * .75f, Color.DeepSkyBlue, new Rectangle(new Point(10, 5), new Point(200, 50))),
                    new InterfaceComponent().CreateButton("Join", Color.Blue * .5f, Color.White,
                        Color.DeepSkyBlue * .75f, Color.DeepSkyBlue, new Rectangle(new Point(10, 5), new Point(200, 50))));
                (hostJoinPrompt.Components[2] as Button).OnClick += (Button sender) =>
                    IPsubmit = ((hostJoinPrompt.Components[1] as TextBox).RenderText);
                Main.UILayer.Components.Add(hostJoinPrompt);
                onError:
                while (IPsubmit == "") { }
                var IP = IPAddress.None;
                try
                {
                    if (IPsubmit == "localhost")
                        IP = IPAddress.Loopback;
                    else
                        IP = IPAddress.Parse(IPsubmit);
                } catch (FormatException) { IPsubmit = ""; goto onError; }
                Main.UILayer.Components.Remove(hostJoinPrompt);
                return IP;
            });
        }

        private void MultiplayerClient_HostStartedGame(string message)
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

        private void MultiplayerClient_OnPlayerCreated(int ID)
        {
            var p = new Player("multiplayerCharacter", ID);
            p.Load(Main.Manager);
            Main.Objects.Add(p);
            if (ID == MultiplayerClient.context.ID)
                Main.Player = p;
        }

        private void MultiplayerClient_RemovedFromMatch(string message)
        {
            System.Windows.Forms.MessageBox.Show("You have been removed from the game");
            Ready = false;
            Main.UpdateLevel(null);
        }

        private void MultiplayerClient_OnLevelChanged(byte[] NewData)
        {
            var l = LevelSave.DeserializeFromServer(NewData);
            System.Windows.Forms.MessageBox.Show("The dining room has changed. Like what you see?");
            Main.UpdateLevel(l);
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
