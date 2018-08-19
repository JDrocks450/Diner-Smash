using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Diner_Smash.UserInterface;

namespace Diner_Smash
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Main : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public static LevelSave SourceLevel;
        public static bool PlacementMode = false;
        public static bool IsDebugMode = false;
        public static Texture2D BaseTexture;
        public static UserInterface UILayer;
        public static Camera GameCamera;
        /// <summary>
        /// A mouse position relative to the camera and zoom.
        /// </summary>
        public static Vector2 MousePosition { get => GameCamera.CalculatedMousePos; }
        public static Random GlobalRandom = new Random();
        public ObjectSpawnList Spawner;
        public FrameCounter frameCounter = new FrameCounter();
        public static Playground GameScene;
        public static DSNetPlay Multiplayer;
        public static List<GameObject> Objects = new List<GameObject>();
        static Queue<GameObject> _waitingObjects = new Queue<GameObject>();
        public static Player Player;
        public static InputHelper GlobalInput;
        public static GameObject ObjectDragging = null;
        public bool GameplayPaused;
        public bool IsConnected { get { try { return Multiplayer.MultiplayerClient.context.Client.Connected; } catch { return false; } } }
        public static bool IsHost { get => Multiplayer.MultiplayerMode == 0; }
        public static bool DEBUG_HighlightingMode = false;
        public static bool FLAG_NetPlayGameStarted = false;
        /// <summary>
        /// Requests that the game begin exiting in a safe way.
        /// </summary>
        public static bool SafeExit;
        public static ContentManager Manager;

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);            
            IsMouseVisible = true;
            IsFixedTimeStep = false;            
            Window.ClientSizeChanged += ClientSizeChanged;
            graphics.PreferMultiSampling = false;
            int w, h;
            w = Properties.DinerSmash.Default.GraphicsWidth;
            h = Properties.DinerSmash.Default.GraphicsHeight;
            if (w == -1 || h == -1)
                graphics.DeviceCreated += (object s, EventArgs e) =>
                {
                    if (w == -1)
                        Properties.DinerSmash.Default.GraphicsWidth = GraphicsDevice.Adapter.CurrentDisplayMode.Width;
                    if (h == -1)
                        Properties.DinerSmash.Default.GraphicsHeight = GraphicsDevice.Adapter.CurrentDisplayMode.Height;
                    Properties.DinerSmash.Default.Save();
                    System.Diagnostics.Process.Start(Environment.CurrentDirectory + "//Diner Smash.exe");
                    Exit();
                };
            else
            {
                graphics.PreferredBackBufferWidth = w;
                graphics.PreferredBackBufferHeight = h;
            }
            switch (Properties.DinerSmash.Default.WindowMode)
            {
                case 2:
                    graphics.ToggleFullScreen();
                    break;
                case 1:
                    Window.IsBorderless = true;
                    break;
                case 0:
                    Window.AllowUserResizing = true;
                    break;
            }            
            Content.RootDirectory = "Content";
            Manager = Content;
#if DEBUG
            IsDebugMode = true;
#endif
        }

        private void ClientSizeChanged(object sender, EventArgs e)
        {
            if (UILayer != null)
                UILayer.Size = Window.ClientBounds.Size;          
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            GlobalInput = new InputHelper();
            GlobalInput.UserInput += UserInputted;
            BaseTexture = new Texture2D(GraphicsDevice, 1, 1);
            BaseTexture.SetData(new Color[1] { Color.White });
            GameScene = new Playground();
            GameCamera = new Camera(GraphicsDevice);
            base.Initialize();
        }        

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);            
            Multiplayer = new DSNetPlay();
            UILayer = new UserInterface(Content, new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
            UpdateLevel(null);                      
        }

        public static void UpdateLevel(LevelSave Save)
        {
            UnloadLevel();           
            Player = null;            
            if (Save is null)
                Save = LevelSave.Load(Manager);
            GameScene.Setup(Manager, Save);
            Main.SourceLevel = Save;
            FinishLoad();
        }

        static bool Loaded { get => Main.SourceLevel != null; }
        /// <summary>
        /// Ran after level save is loaded.
        /// </summary>
        protected static void FinishLoad()
        {
            foreach (var obj in Main.Objects)
                obj.Load(Manager);
            if (!Multiplayer.Ready)
                Multiplayer.PromptHostJoin();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            
        }

        public static void AddObject(GameObject Object)
        {
            _waitingObjects.Enqueue(Object);
        }

        public static void UnloadLevel()
        {
            var l = Objects.ToArray();
            foreach(var i in l)
            {
                i.Dispose();
            }
            Objects.Clear();
            SourceLevel = null;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                SafeExit = true;
            if (SafeExit)
                Exit();
            GameCamera.Update();
            double delta = gameTime.ElapsedGameTime.TotalSeconds;
            if (delta == 0)
            {
                base.Update(gameTime);
                return;
            }
            GlobalInput.Listen();
            UILayer.Update(gameTime);            
            if (UILayer.Components.Find(x => x.Exclusive) != null)
                GameplayPaused = true;
            else
                GameplayPaused = false;            
            if (Loaded && !GameplayPaused)
            {
                try //If this collection throws an exception it's not a big deal.
                {
                    foreach (var obj in _waitingObjects)
                        Objects.Add(_waitingObjects.Dequeue());
                }
                catch { }
                try
                {
                    foreach (var obj in Objects)
                    {
                        if (obj is Player)
                        {
                            obj.Update(gameTime);
                            continue;
                        }
                        if (FLAG_NetPlayGameStarted)
                            obj.Update(gameTime);
                    }
                }                
                catch (Exception e) { }
        }
            if (PlacementMode)
            {
                if (Spawner is null)
                    Spawner = new ObjectSpawnList();
            }
            else
                Spawner = null;
            if (!IsDebugMode && DEBUG_HighlightingMode)
                DEBUG_HighlightingMode = false;
            //Performs a check to see which object was clicked factoring in draw-order.
            GlobalInput.CollisionCheck(Mouse.GetState(), Main.Objects);
            frameCounter.Update(gameTime);
            DisplayDEBUGInfo(gameTime);           
            base.Update(gameTime);
        }

        private void UserInputted(InputHelper.InputEventArgs e)
        {
            if (e.PressedKeys.Contains(Keys.F5))
                SourceLevel.Save();
            if (e.PressedKeys.Contains(Keys.F6))
                PlacementMode = !PlacementMode;
            if (e.PressedKeys.Contains(Keys.F1))
                IsDebugMode = !IsDebugMode;
            if (e.PressedKeys.Contains(Keys.F3))
            {
                Lighting.LightColor = Color.Orange;
                Lighting.LightIntensity += .05f;
                DEBUG_HighlightingMode = !DEBUG_HighlightingMode;
            }
        }

        UserInterface.StackPanel DEBUGInformationStackPanel = new UserInterface.StackPanel();
        public void DisplayDEBUGInfo(GameTime gameTime)
        {            
            if (DEBUGInformationStackPanel.Components.Count == 0)
            {
                DEBUGInformationStackPanel.CreateImage(BaseTexture, Color.Black * .75f, new Rectangle(10, 10, 0, 0));
                DEBUGInformationStackPanel.
                    AddRange(InterfaceComponent.HorizontalLock.Left, !IsDebugMode ? new InterfaceComponent[] { frameCounter.Format()[0] } : frameCounter.Format());
                DEBUGInformationStackPanel.AddToParent(UILayer);
            }
            frameCounter.Format();
            DEBUGInformationStackPanel.Reformat();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            if (Loaded)
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, GameCamera.Transform());
                GameScene.Draw(spriteBatch);
                if (Main.IsDebugMode)
                    Player?.PathFinder.DEBUG_DrawMap(spriteBatch);
                foreach (var obj in Objects)
                    obj.Draw(spriteBatch);
                spriteBatch.End();
            }
            spriteBatch.Begin(SpriteSortMode.Immediate);
            UILayer.Draw(spriteBatch);            
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}