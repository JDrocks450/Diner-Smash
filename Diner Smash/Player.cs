using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Diner_Smash.PathHelper;

namespace Diner_Smash
{
    [Serializable]
    public class Player : GameObject
    {
        public enum PlayerNameTable { Idle, Menu1, Dinner1, Dinner2 };
        [XmlIgnore]
        public static Point FrameSize;
        public const float SCALE = .45f;
        public const int MAX_PLATES = 2;
        public static Color[] PLAYER_COLORS = new Color[] { Color.White, Color.LightPink, Color.PaleGreen, Color.Yellow };

        int MAX_FRAME = -1;
        int Frame
        {
            get => _frame;
            set
            {
                _frame = value;
                _frameChanged = true;
            }
        }

        public int OwnerID { get; }
        private int _frame = 0;
        private bool _frameChanged;
        [NonSerialized]
        Texture2D[] Frames;

        [NonSerialized]
        public int Plates = 0;

        public GameObject[] Hands = new GameObject[2];

        public Point TranslateLocationtoNavNodeLocation()
        {
            return new Point((int)Location.X + (Width / 2), (int)Location.Y + Height);
        }
        
        [NonSerialized]
        /// <summary>
        /// Each task to be performed by the player one after another.
        /// (Get food --> Walk to Table --> Serve Food)
        /// </summary>        
        public List<Task> Tasks = new List<Task>();
        [NonSerialized]
        public bool TaskRunning = false;
        public PathHelper PathFinder;

        public Player(string Name, int ClientID) : base (Name, ObjectNameTable.Player)
        {
            Main.GlobalInput.UserInput += UserInputted;
            Width = FrameSize.X;
            Height = FrameSize.Y;
            IsRoutable = false;
            IsInteractable = false;
            PathFinder = new PathHelper(ref Main.Objects, 50, 100);
            DrawIndex = 0f;
            OwnerID = ClientID;
            Scale = SCALE;
        }

        public bool PlaceObjectInHand(GameObject Object)
        {
            if (Hands[0] == null)
                Hands[0] = Object;
            else if (Hands[1] == null)
                Hands[1] = Object;
            else
                return false;              
            return true;
        }

        public bool RemoveObjectFromHand(GameObject Object)
        {
            try
            {
                Hands[Array.IndexOf(Hands, Object)] = null;
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void UserInputted(InputHelper.InputEventArgs e)
        {
            if (Main.PlacementMode)
            {
                if (e.PressedKeys.Contains(Keys.Right))
                    Frame++;
                else if (e.PressedKeys.Contains(Keys.Left))
                    Frame--;                
                _frameChanged = true;
            }
            if (e.MouseLeftClick && !Main.Objects.Where(x => x.IsMouseOver).Any())
            {                
                var p = Main.MousePosition.ToPoint();
                Tasks.Add(new Task(() => RequestNavigation(p)));
            }
        }

        public override void Load(ContentManager Content)
        {
            MAX_FRAME = Enum.GetNames(typeof(PlayerNameTable)).Count();
            Frames = new Texture2D[MAX_FRAME];
            for(int i = MAX_FRAME-1; i >= 0; i--)
                Frames[i] = Content.Load<Texture2D>("Entities/Player/" + Enum.GetNames(typeof(PlayerNameTable))[i]);
            Frame = 0;            
            base.Load(Content);
        }

        public void RequestNavigation(Point Destination)
        {
            if (Main.PlacementMode)
                return;
            if (_walking)
                return;
            _walking = true;
            Main.Multiplayer.NavigationRequest(this, Destination);
            while (_walking != false) //wait for Update to handle the navigation task.
            { }
        }
        public void RequestInteraction(int OBJID)
        {
            Main.Multiplayer.InteractionRequest(this, OBJID);
        }

        bool _walking = false;
        public List<NavNode> CurrentRoute;
        NavNode Current;
        int _pathNodeIndex = 0;
        float _currentPercent = 0f;
        Point _from;
        public void WalkToPoint(Point Destination)
        {
            _walking = true;
            var start = new Point(BoundingRectangle.Center.X, BoundingRectangle.Bottom);
            CurrentRoute = PathFinder.Route(start, Destination);
            if (!PathFinder.Successful)
            {
                _walking = false;
                return;
            }
            _pathNodeIndex = 0;
            Current = CurrentRoute[0]; //Submit request to Update() to navigate the player.
            while (Current != null) //wait for Update to handle the navigation task.
            { }
            _walking = false;
        }

        public override void Update(GameTime gameTime)
        {
            if (Tasks.Any() && !TaskRunning)
            {            
                Tasks[0].ContinueWith((Task c) =>
                {
                    Tasks.Remove(c);
                    TaskRunning = false;
                });                
                TaskRunning = true;
                Tasks[0].Start();
            }
            if (Frame > MAX_FRAME - 1)
                Frame = MAX_FRAME-1;
            else if (Frame < 0)
                Frame = 0;
            ID = -1;
            if (OwnerID <= PLAYER_COLORS.Length)
                Mask = PLAYER_COLORS[OwnerID - 1];
            else
                Mask = Color.Green;
            if (Hands[1] == null && Hands[0] != null)
            {
                if (Hands[0].Identity == ObjectNameTable.Food)
                    Frame = 2;
                else if (Hands[0].Identity == ObjectNameTable.Menu)
                    Frame = 1;
                else
                    Frame = 0;
            }
            else
                Frame = 0;
            // PATH FINDING HERE!!
            if (Current != null)
            {
                if (_currentPercent == 0)
                {
                    if (_pathNodeIndex == CurrentRoute.Count || !PathFinder.Successful)
                    {
                        Current = null;
                        CurrentRoute = null;
                        goto escape;
                    }
                    var next = CurrentRoute[_pathNodeIndex++];
                    _from = new Point(Current.Location.X - (Width/2), Current.Location.Y - Height);
                    Current = next;
                }
                var _next = new Point(Current.Location.X - (Width / 2), Current.Location.Y - Height);
                _currentPercent += .05f;
                Location = Vector2.SmoothStep(_from.ToVector2(), _next.ToVector2(), _currentPercent);
                if (_currentPercent >= 1f)
                    _currentPercent = 0f;
            }            
            escape:
            if (CurrentRoute?.Count == 0)
                CurrentRoute = null;
            PathFinder.Update();
            // PATH FINDING HERE!! 
            if (_frameChanged)
            {
                Texture = Frames[Frame];
                FrameSize = new Point(Texture.Width, Texture.Height);
                base.Load();
                _frameChanged = false;
            }
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Main.IsDebugMode && CurrentRoute != null)
                PathFinder.DEBUG_DrawRoute(spriteBatch, CurrentRoute);            
            base.Draw(spriteBatch);
        }
    }
}
