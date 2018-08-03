﻿using Microsoft.Xna.Framework;
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
        public static bool PlacementMode = true;
        public static bool IsDebugMode = false;
        public static Texture2D BaseTexture;
        public static UserInterface UILayer;
        public static Camera GameCamera = new Camera();
        /// <summary>
        /// A mouse position relative to the camera and zoom.
        /// </summary>
        public static Point MousePosition;
        public static Random GlobalRandom = new Random();
        public ObjectSpawnList Spawner;
        public FrameCounter frameCounter = new FrameCounter();
        public static Playground GameScene;
        public static MultiplayerHandler Multiplayer;
        public static List<GameObject> Objects = new List<GameObject>();
        static Queue<GameObject> _waitingObjects = new Queue<GameObject>();
        public static Player Player;
        public static InputHelper GlobalInput;
        public static GameObject ObjectDragging = null;
        public bool GameplayPaused;
        public bool IsConnected { get { try { return Multiplayer.MultiplayerClient.context.Client.Connected; } catch { return false; } } }
        public static bool IsHost { get => Multiplayer.MultiplayerMode == 0; }
        public static bool FLAG_NetPlayGameStarted = false;

        public static ContentManager Manager;

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            IsMouseVisible = true;
            IsFixedTimeStep = false;
            //graphics.ToggleFullScreen();
            Content.RootDirectory = "Content";
            Manager = Content;
#if DEBUG
            IsDebugMode = true;
#endif
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            GlobalInput = new InputHelper();
            GlobalInput.UserInput += UserInputted;
            BaseTexture = new Texture2D(GraphicsDevice, 1, 1);
            BaseTexture.SetData(new Color[1] { Color.White });
            GameScene = new Playground();            
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
            Multiplayer = new MultiplayerHandler();
            UILayer = new UserInterface(Content, new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
            Spawner = new ObjectSpawnList(Content, new Point(0));
            Spawner.Formatter.Location = new Point(GraphicsDevice.Viewport.Width - Spawner.Formatter.Destination.Width - 10, 10);
            Spawner.Formatter.Reformat(UILayer.Font);
            UpdateLevel(null);                      
        }

        public static void UpdateLevel(LevelSave Save)
        {
            Objects.Clear();            
            Player = null;            
            if (Save is null)
                Save = LevelSave.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "test.xml"), Manager);
            GameScene.Setup(Manager, Save);
            FinishLoad();
        }

        static bool _loaded = false;
        /// <summary>
        /// Ran after level save is loaded.
        /// </summary>
        protected static void FinishLoad()
        {
            foreach (var obj in Main.Objects)
                obj.Load(Manager);
            if (!Multiplayer.Ready)
                Multiplayer.PromptUser();
            _loaded = true;
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

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            MousePosition = Mouse.GetState().Position + GameCamera.DesiredPosition.ToPoint();
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
            if (_loaded && !GameplayPaused)
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
                catch { }
            }
            if (PlacementMode)
            {
                if (!UILayer.Components.Contains(Spawner))                
                    UILayer.Components.Add(Spawner);                                    
                Spawner.Update(gameTime);                
            }                      
            frameCounter.Update(gameTime);
            DisplayDEBUGInfo();           
            base.Update(gameTime);
        }

        private void UserInputted(InputHelper.InputEventArgs e)
        {
            if (e.PressedKeys.Contains(Keys.F5))
                GameScene.Source.Save(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "test.xml"));
            if (e.PressedKeys.Contains(Keys.F6))
                PlacementMode = !PlacementMode;
            if (e.PressedKeys.Contains(Keys.F1))
                IsDebugMode = !IsDebugMode;
        }

        UserInterface.StackPanel DEBUGInformationStackPanel = new UserInterface.StackPanel();
        public void DisplayDEBUGInfo()
        {
            if (DEBUGInformationStackPanel.Components.Count == 0)
            {
                DEBUGInformationStackPanel.CreateImage(BaseTexture, Color.Black * .75f, new Rectangle(10, 10, 0, 0));
            }
            else
                DEBUGInformationStackPanel.Components.Clear();
            if (IsDebugMode)
            DEBUGInformationStackPanel.AddRange(false,
                new UserInterface.InterfaceComponent().CreateText(PlacementMode ? "PLACEMENT MODE" : "DEBUG MODE", Color.White, new Point(10)));
            DEBUGInformationStackPanel.
                AddRange(false, !IsDebugMode ? new InterfaceComponent[] { frameCounter.Format()[0] } : frameCounter.Format());
            try
            {
                if (IsDebugMode)
                foreach (var c in Objects.Where(x => x is Player))
                {
                    foreach (var t in c.ReturnDebugInfo())
                        DEBUGInformationStackPanel.AddRange(false,
                            new UserInterface.InterfaceComponent().CreateText(t.Replace("*", ""), Color.White,
                            t.StartsWith("*") ? new Point(10, 10) : new Point(15, 5)));
                }
            }
            catch { }
            DEBUGInformationStackPanel.Reformat(UILayer.Font);
            if (!UILayer.Components.Contains(DEBUGInformationStackPanel))
                UILayer.Components.Add(DEBUGInformationStackPanel);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (_loaded)
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, GameCamera.Transform(GraphicsDevice));
                GameScene.Draw(spriteBatch);
                if (Main.IsDebugMode)
                    Player?.PathFinder.DEBUG_DrawMap(spriteBatch);
                try
                {
                    foreach (var obj in Objects)
                        obj.Draw(spriteBatch);
                }
                catch { }
                spriteBatch.End();
            }
            spriteBatch.Begin(SpriteSortMode.Immediate);
            UILayer.Draw(spriteBatch);            
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}