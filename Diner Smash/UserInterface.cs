﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Diner_Smash.UserInterface;

namespace Diner_Smash
{
    public class InterfaceFont
    {
        public const float DEFSIZE = 24f;

        static SpriteFont RegFont;
        static SpriteFont BoldFont;

        public SpriteFont RenderFont
        {
            get
            {
                switch (Style)
                {
                    case Styles.Regular:
                        return RegFont;
                    case Styles.Bold:
                        return BoldFont;
                    default:
                        return RegFont;
                }
            }
        }
        public float RenderSize { get; internal set; }
        float Scale
        {
            get => RenderSize / DEFSIZE;
        }

        public Vector2 Measure(string text)
        {
            return (RenderFont.MeasureString(text)*Scale);
        }

        public enum Styles
        {
            Regular,
            Bold
        }
        public Styles Style { get; internal set; }

        public static void LoadFonts()
        {
            RegFont = Main.Manager.Load<SpriteFont>("Font");
            BoldFont = Main.Manager.Load<SpriteFont>("Bold");
        }

        public InterfaceFont(float Size = 12f, Styles Style = Styles.Regular)
        {
            this.Style = Style;
            RenderSize = Size;            
        }        

        public void DrawString(SpriteBatch batch, string text, Vector2 Location, Color color)
        {
            batch.DrawString(RenderFont, text, Location, color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }

    public class InterfaceComponent
    {
        public delegate void InvalidatedHandler();
        public event InvalidatedHandler Invalidated;

        public enum VerticalLock
        {
            None,
            Top,
            Center,
            Bottom
        }
        public VerticalLock VLock;
        public enum HorizontalLock
        {
            None,
            Left,
            Center,
            Right
        }
        public HorizontalLock HLock;

        public InterfaceParentComponent Parent;
        public bool Exclusive { get; set; }
        public bool AutoSizeWidth;
        public bool AutoSizeHeight;
        public object Tag;

        public virtual ObjectContext.AvailablityStates Availablity
        {
            get;
            internal set;
        }
        public enum Render
        {
            Texture2D,
            InterfaceFont,
            None
        }
        public Render GetRender
        {
            get;
            internal set;
        } = Render.None;

        public Texture2D RenderTexture;
        public Rectangle Destination
        {
            get => new Rectangle(LiteralLocation, Size);
            set
            {
                LiteralLocation = value.Location;
                Size = value.Size;
                Invalidated?.Invoke();
            }
        }

        /// <summary>
        /// The desired location within the parent control.
        /// </summary>
        public Point Margin
        {
            get => new Point(X, Y);
            set
            {
                var shouldinvalidate = false;
                if (value.X != X || value.Y != Y)
                    shouldinvalidate = true;
                X = value.X;
                Y = value.Y;
                if (shouldinvalidate)
                    Invalidated?.Invoke();
            }
        }

        public Point Size
        {
            get => new Point(Width, Height);
            set
            {
                var shouldinvalidate = false;
                if (value.X != Width || value.Y != Height)
                    shouldinvalidate = true;
                Width = value.X;
                Height = value.Y;
                if (shouldinvalidate)
                    Invalidated?.Invoke();
            }
        }

        public int Width;
        public int Height;
        /// <summary>
        /// Margin's X value
        /// </summary>
        public int X;
        /// <summary>
        /// Margin's Y value
        /// </summary>
        public int Y;

        /// <summary>
        /// Gets the point on the screen in pixels of the component.
        /// </summary>
        public Point LiteralLocation { get; internal set; }

        public Point STACKPANEL_Padding;
        public string RenderText;
        public float TextSize;
        public Color MaskingColor = Color.White;
        public InterfaceFont Font = new InterfaceFont();

        public InterfaceComponent CreateText(string Text, Color MaskingColor, Point Location, bool ChangeLocation = true)
        {
            RenderText = Text;
            this.MaskingColor = MaskingColor;
            if (ChangeLocation)
                Destination = new Rectangle(Location, Point.Zero);
            GetRender = Render.InterfaceFont;
            return this;
        }

        public InterfaceComponent CreateText(InterfaceFont Font, string Text, Color MaskingColor, Point Location)
        {
            RenderText = Text;
            this.MaskingColor = MaskingColor;
            Destination = new Rectangle(Location, Point.Zero);
            GetRender = Render.InterfaceFont;
            this.Font = Font;
            return this;
        }

        public InterfaceComponent CreateImage(Texture2D Texture, Color color, Rectangle destRect)
        {
            RenderTexture = Texture;
            MaskingColor = color;
            Destination = destRect;
            GetRender = Render.Texture2D;
            return this;
        }

        public Button CreateButton(string Text, Color BackgroundColor, Color ForeColor, Color Highlight, Color Click, Rectangle Space)
        {
            return new Button().CreateButton(Text, BackgroundColor, ForeColor, Highlight, Click, Space);
        }

        public TextBox CreateTextBox(string Text, Color BackgroundColor, Color ForeColor, Color Highlight, Color Click, Rectangle Space)
        {
            return new TextBox().CreateTextBox(Text, BackgroundColor, ForeColor, Highlight, Click, Space);
        }

        public virtual void AddToParent(InterfaceParentComponent Parent)
        {
            Availablity = ObjectContext.AvailablityStates.Enabled;
            this.Parent = Parent;
            Parent.Components.Add(this);
        }

        /// <summary>
        /// Prepares the component to be removed from its parent.
        /// </summary>
        /// <param name="remove">If false, the component will prepare to be disabled rather than be removed from parent.</param>
        public virtual void RemoveFromParent(bool remove)
        {
            Availablity = ObjectContext.AvailablityStates.Disabled;
            if (remove)
                Parent.Components.Remove(this);
        }

        public void SetCenterScreen()
        {
            HLock = HorizontalLock.Center;
            VLock = VerticalLock.Center;
        }

        public virtual void AutoSize()
        {
            if (AutoSizeWidth)
                Width = Parent.Width - (STACKPANEL_Padding.X * 2);
            if (AutoSizeHeight)
                Height = Parent.Height - (STACKPANEL_Padding.Y * 2);
        }

        public virtual void Update(GameTime gameTime)
        {
            if (Availablity == ObjectContext.AvailablityStates.Disabled)
                return;
            var newLoc = Margin;
            switch (HLock)
            {                
                case HorizontalLock.Left:
                    newLoc.X = 0;
                    break;
                case HorizontalLock.Center:
                    newLoc.X = Parent.Destination.Width / 2 - Size.X / 2;
                    break;
                case HorizontalLock.Right:
                    newLoc.X = Parent.Destination.Width - Size.X;
                    break;
            }
            switch (VLock)
            {
                case VerticalLock.Top:
                    newLoc.Y = 0;
                    break;
                case VerticalLock.Center:
                    newLoc.Y = Parent.Destination.Height / 2 - Size.Y / 2;
                    break;
                case VerticalLock.Bottom:
                    newLoc.Y = Parent.Destination.Height - Size.Y;
                    break;
            }
            newLoc += Parent.Destination.Location;
            LiteralLocation = newLoc;
            AutoSize();
        }

        public virtual void Draw(SpriteBatch sprite)
        {
            if (Availablity != ObjectContext.AvailablityStates.Invisible)
                switch (GetRender)
                {
                    case InterfaceComponent.Render.InterfaceFont:
                        Font.DrawString(sprite, RenderText, Destination.Location.ToVector2(), MaskingColor);
                        break;
                    case InterfaceComponent.Render.Texture2D:
                        sprite.Draw(RenderTexture, Destination, MaskingColor);
                        break;
                }
        }
    }

    public class InterfaceParentComponent : InterfaceComponent
    {
        public List<InterfaceComponent> Components { get; set; }
        public InterfaceParentComponent()
        {
            Components = new List<InterfaceComponent>();
        }

        /// <summary>
        /// The parent's availablity applies to children as well.
        /// </summary>
        public override ObjectContext.AvailablityStates Availablity
        {
            get => base.Availablity;
            internal set
            {
                base.Availablity = value;
                foreach (var i in Components)
                    i.Availablity = value;
            }
        }

        public override void AutoSize()
        {
            if (AutoSizeWidth)
                Width = Components.Select(x => x.Width).Max() + STACKPANEL_Padding.X * 2;
            if (AutoSizeHeight)
                Height = Components.Select(x => x.Y + x.Height).Max() + STACKPANEL_Padding.Y;
        }        

        /// <summary>
        /// Clears out any null child components
        /// </summary>
        public void CleanComponents()
        {
            Components = Components.FindAll(x => x != null);
        }

        public override void RemoveFromParent(bool remove = true)
        {
            foreach (var c in Components)
                c.RemoveFromParent(false);
            base.RemoveFromParent(remove);
        }

        bool _dialogHolding = false;

        /// <summary>
        /// Displays the control and it's contents as a thread-blocking dialog.
        /// </summary>
        public Task ShowAsDialog()
        {
            return Task.Run(() =>
                {
                    Exclusive = true;
                    _dialogHolding = true;
                    AddToParent(Main.UILayer);
                    while (_dialogHolding) { }
                    RemoveFromParent();
                    Exclusive = false;
                });
        }

        /// <summary>
        /// Closes this component if ShowDialog was called prior.
        /// </summary>
        public void CloseDialog()
        {
            _dialogHolding = false;
        }
    }

    public class UserInterface : InterfaceParentComponent
    {
        public class ObjectSpawnList : StackPanel
        {
            public ContentManager Content { get => Main.Manager; }

            public ObjectSpawnList()
            {
                var objsList = new InterfaceComponent[Enum.GetNames(typeof(GameObject.ObjectNameTable)).Count() + 1];
                int i = 1;
                objsList[0] = new InterfaceComponent().CreateText("SPAWN OBJECT", Color.White, new Point(15));
                foreach (var s in Enum.GetNames(typeof(GameObject.ObjectNameTable)))
                {
                    objsList[i] = new InterfaceComponent().CreateButton(s,
                        Color.Black * .3f,
                        Color.White,
                        Color.White * .5f,
                        Color.DeepSkyBlue,
                        new Rectangle(i == 1 ? new Point(10) : new Point(10, 2), new Point(175, 50)));
                    objsList[i].Tag = Enum.Parse(typeof(GameObject.ObjectNameTable), s);
                    (objsList[i] as Button).OnClick += ObjectSpawnList_OnClick;
                    i++;
                }
                AddRange(true, objsList);
                HLock = HorizontalLock.Right;
                Main.GlobalInput.UserInput += GlobalInput_UserInput;
                AddToParent(Main.UILayer);
            }

            private void GlobalInput_UserInput(InputHelper.InputEventArgs e)
            {
                if (Availablity != ObjectContext.AvailablityStates.Disabled)
                    if (e.PressedKeys.Contains(Keys.F2))
                        if (Availablity == ObjectContext.AvailablityStates.Enabled)
                            Availablity = ObjectContext.AvailablityStates.Invisible;
                        else
                            Availablity = ObjectContext.AvailablityStates.Enabled;
            }

            private void ObjectSpawnList_OnClick(Button sender)
            {
                var enumval = (ObjectContext.ObjectNameTable)sender.Tag;
                var value = GameObject.Create("object", enumval, Content);
                value.Load(Content);
                value.Location = Main.GameCamera.DesiredPosition + (Main.UILayer.Size / new Point(2)).ToVector2() - (value.Size / new Point(2)).ToVector2();
                Main.AddObject(value);
            }
        }

        public class TextBox : InterfaceComponent
        {
            public delegate void OnTextAcceptedHandler(object sender);
            public event OnTextAcceptedHandler Accepted;

            public Color Background, Foreground, High, Active;
            public bool IsMouseOver = false;
            private bool _active;

            public int CursorPosition
            {
                get; internal set;
            }
            public int TextLength { get; internal set; }
            public bool IsActive
            {
                get => _active; set { _active = value; }
            }

            /// <summary>
            /// Creates a textbox with the default settings.
            /// </summary>
            public TextBox()
            {
                Availablity = ObjectContext.AvailablityStates.Disabled;
                CreateTextBox("Untitled", Color.Black * .75f, Color.White, Color.Gray * .75f, Color.Gray,
                new Rectangle(0, 0, 500, 50));
            }

            public new TextBox CreateTextBox(string Text, Color BackgroundColor, Color ForeColor, Color Highlight, Color Active, Rectangle Space)
            {
                RenderText = Text;
                Background = BackgroundColor;
                Foreground = ForeColor;
                High = Highlight;
                this.Active = Active;
                Destination = Space;
                if (Availablity != ObjectContext.AvailablityStates.Enabled)
                    Main.GlobalInput.UserInput += GlobalInput_UserInput;
                GetRender = Render.Texture2D;
                Availablity = ObjectContext.AvailablityStates.Enabled;
                return this;
            }

            bool UpperCase = false;
            private void GlobalInput_UserInput(InputHelper.InputEventArgs e)
            {
                if (e.MouseLeftClick)
                {
                    if (IsMouseOver)
                        IsActive = true;
                    else
                        IsActive = false;
                }
                if (IsActive && e.PressedKeys.Any())
                {
                    var old = RenderText.Length;
                    var changes = "";                    
                    foreach(var k in e.PressedKeys)
                    {
                        var letter = Enum.GetName(typeof(Keys), k);
                        if (letter.Where(x => char.IsNumber(x)).Any())
                            letter = new string(letter.Where(x => char.IsNumber(x)).ToArray());
                        switch (k)
                        {
                            case Keys.Back:                                
                            case Keys.Delete:
                                continue;
                            case Keys.RightShift:
                            case Keys.LeftShift:
                                continue;
                            case Keys.Left:
                            case Keys.Right:
                                continue;
                            case Keys.Up:
                                CursorPosition=RenderText.Length;
                                continue;
                            case Keys.Down:
                                CursorPosition = 0;
                                continue;
                            case Keys.Space:
                                letter = " ";
                                break;
                            case Keys.OemPeriod:
                                letter = ".";
                                break;
                            case Keys.Enter:
                                Accepted?.Invoke(this);
                                return;
                        }                        
                        if (UpperCase)
                            letter = letter.ToUpper();
                        else
                            letter = letter.ToLower();
                        RenderText = RenderText.Insert(CursorPosition, letter);
                        changes += letter;
                    }                    
                    var _new = RenderText.Length;
                    CursorPosition += _new - old;                    
                }
            }

            public bool CursorVisible = true;
            public const float BLINK_INTERVAL = .7f;

            TimeSpan _timeSinceLastHold;
            TimeSpan _timeSinceBlinkChange;
            bool canHold;
            public override void Update(GameTime gameTime)
            {
                if (Availablity == ObjectContext.AvailablityStates.Disabled)
                    return;
                var mouse = Mouse.GetState();
                var MouseRect = new Rectangle(mouse.Position, new Point(1, 1));
                IsMouseOver = false;
                if (MouseRect.Intersects(Destination))
                    IsMouseOver = true;
                if (canHold && IsActive)
                {
                    UpperCase = Keyboard.GetState().CapsLock;
                    foreach (var k in Keyboard.GetState().GetPressedKeys())
                        switch (k)
                        {
                            case Keys.Back:
                                if (CursorPosition > 0)
                                {
                                    RenderText = RenderText.Remove(CursorPosition - 1, 1);
                                    CursorPosition--;
                                }
                                continue;
                            case Keys.Delete:
                                if (CursorPosition < TextLength)
                                    RenderText = RenderText.Remove(CursorPosition, 1);
                                continue;
                            case Keys.RightShift:
                            case Keys.LeftShift:
                                UpperCase = !UpperCase;
                                continue;
                            case Keys.Left:
                                if (CursorPosition > 0)
                                    CursorPosition--;
                                continue;
                            case Keys.Right:
                                if (CursorPosition < RenderText.Length)
                                    CursorPosition++;
                                continue;
                        }
                    canHold = false;                    
                }                
                if (IsActive)
                {
                    TextLength = RenderText.Length;
                    if (CursorPosition < 0)
                        CursorPosition = 0;
                    if (CursorPosition > TextLength)
                        CursorPosition = TextLength;
                }
                if (!canHold)
                {
                    _timeSinceLastHold += gameTime.ElapsedGameTime;
                    if (_timeSinceLastHold.TotalSeconds > .07f)
                    {
                        canHold = true;
                        _timeSinceLastHold = TimeSpan.Zero;
                    }
                }
                _timeSinceBlinkChange += gameTime.ElapsedGameTime;
                if (_timeSinceBlinkChange.TotalSeconds > BLINK_INTERVAL && IsActive)
                {
                    CursorVisible = !CursorVisible;
                    _timeSinceBlinkChange = TimeSpan.Zero;
                }
                else if (!IsActive)
                    CursorVisible = IsActive;
                base.Update(gameTime);
            }

            public override void Draw(SpriteBatch sprite)
            {
                Color DrawColor = Background;
                if (IsMouseOver && Mouse.GetState().LeftButton != ButtonState.Pressed) //Hover
                    DrawColor = High;
                if (IsMouseOver && Mouse.GetState().LeftButton == ButtonState.Pressed) //Hover
                    DrawColor = High;
                if (IsActive) //Active
                    DrawColor = Active;
                sprite.Draw(Main.BaseTexture, Destination, DrawColor);
                var cursorloc = Font.Measure(RenderText.Substring(0, CursorPosition)).ToPoint();
                var textSize = Font.Measure(RenderText);
                if (textSize.Y == 0)
                    textSize.Y = Font.Measure("REMY").Y;
                Font.DrawString(sprite, RenderText,
                    new Vector2(Destination.X + (int)(Destination.Height - textSize.Y),
                    Destination.Y + (Destination.Height / 2) - (int)(textSize.Y / 2)),
                    Foreground);
                if (CursorVisible)
                    sprite.Draw(Main.BaseTexture, new Rectangle(Destination.X + (Destination.Height - (int)textSize.Y) + cursorloc.X,
                        Destination.Y + (Destination.Height / 2) - (int)(textSize.Y / 2), 2, (int)textSize.Y), Foreground); //Draw cursor
            }
        }

        public class Button : InterfaceComponent
        {
            public delegate void OnClickEventHandler(Button sender);
            public event OnClickEventHandler OnClick;
            
            public Color Background, Foreground, High, Click;
            public bool IsMouseOver = false;

            public new Button CreateButton(string Text, Color BackgroundColor, Color ForeColor, Color Highlight, Color Click, Rectangle Space)
            {
                RenderText = Text;
                Background = BackgroundColor;
                Foreground = ForeColor;
                High = Highlight;
                this.Click = Click;
                Destination = Space;
                Main.GlobalInput.UserInput += GlobalInput_UserInput;
                GetRender = Render.Texture2D;
                return this;
            }

            private void GlobalInput_UserInput(InputHelper.InputEventArgs e)
            {
                if (Availablity != ObjectContext.AvailablityStates.Disabled)
                    if (e.MouseLeftClick)
                    {
                        if (IsMouseOver)
                            OnClick?.Invoke(this);
                    }
            }

            public override void RemoveFromParent(bool remove)
            {
                IsMouseOver = false;
                base.RemoveFromParent(remove);
            }

            public override void Update(GameTime gameTime)
            {
                if (Availablity == ObjectContext.AvailablityStates.Disabled)
                    return;
                var mouse = Mouse.GetState();
                var MouseRect = new Rectangle(mouse.Position, new Point(1, 1));
                IsMouseOver = false;
                if (MouseRect.Intersects(Destination))
                    IsMouseOver = true;
                base.Update(gameTime);
            }

            public override void Draw(SpriteBatch sprite)
            {
                if (Availablity != ObjectContext.AvailablityStates.Invisible)
                {
                    Color DrawColor = Background;
                    if (IsMouseOver && Mouse.GetState().LeftButton != ButtonState.Pressed) //Hover
                        DrawColor = High;
                    if (IsMouseOver && Mouse.GetState().LeftButton == ButtonState.Pressed) //Hover
                        DrawColor = Click;
                    sprite.Draw(Main.BaseTexture, Destination, DrawColor);
                    var textSize = Font.Measure(RenderText);
                    Font.DrawString(sprite, RenderText,
                        new Vector2(Destination.X + (Destination.Width / 2) - (int)(textSize.X / 2),
                        Destination.Y + (Destination.Height / 2) - (int)(textSize.Y / 2)),
                        Foreground);
                }
            }
        }

        public class FrameCounter
        {
            public long TotalFrames { get; private set; }
            public double TotalSeconds { get; private set; }
            public double AverageFramesPerSecond { get; private set; }
            public double CurrentFramesPerSecond { get; private set; }
            public TimeSpan TotalGameTime;

            public const int MAXIMUM_SAMPLES = 100;

            private Queue<double> _sampleBuffer = new Queue<double>();

            public void Update(GameTime gameTime)
            {
                CurrentFramesPerSecond = 1.0d / gameTime.ElapsedGameTime.TotalSeconds;
                CurrentFramesPerSecond = Math.Truncate(CurrentFramesPerSecond);
                _sampleBuffer.Enqueue(CurrentFramesPerSecond);

                if (_sampleBuffer.Count > MAXIMUM_SAMPLES)
                {
                    _sampleBuffer.Dequeue();
                    AverageFramesPerSecond = _sampleBuffer.Average(i => i);
                }
                else
                {
                    AverageFramesPerSecond = CurrentFramesPerSecond;
                }

                TotalFrames++;
                TotalGameTime = gameTime.TotalGameTime;
                TotalSeconds += gameTime.ElapsedGameTime.TotalSeconds;
            }

            InterfaceComponent[] formatBuffer;
            public InterfaceComponent[] Format()
            {
                if (formatBuffer == null)
                {
                    formatBuffer = new InterfaceComponent[]
                    {
                        new InterfaceComponent().CreateText($"FPS: {CurrentFramesPerSecond}", CurrentFramesPerSecond > 30 ? Color.Green : Color.Red, new Point(10)),
                        new InterfaceComponent().CreateText($"Total Frames: {TotalFrames}", Color.White, new Point(10, 5)),
                        new InterfaceComponent().CreateText($"Average FPS: {AverageFramesPerSecond}", Color.White, new Point(10, 5)),
                        new InterfaceComponent().CreateText(string.Format("Game Time: {0:hh\\:mm\\:ss}", TotalGameTime), Color.White, new Point(10, 5))
                    };
                    return formatBuffer;
                }
                formatBuffer[0].CreateText($"FPS: {CurrentFramesPerSecond}", CurrentFramesPerSecond > 30 ? Color.Green : Color.Red, new Point(10), false);
                formatBuffer[1].CreateText($"Total Frames: {TotalFrames}", Color.White, new Point(10, 5), false);
                formatBuffer[2].CreateText($"Average FPS: {AverageFramesPerSecond}", Color.White, new Point(10, 5), false);
                formatBuffer[3].CreateText(string.Format("Game Time: {0:hh\\:mm\\:ss}", TotalGameTime), Color.White, new Point(10, 5), false);
                return formatBuffer;
            }
        }

        public class StackPanel : InterfaceParentComponent
        {
            public StackPanel(Color Background = default, bool UseDefaultBackground = true)
            {
                if (UseDefaultBackground)
                {
                    this.RenderTexture = Main.BaseTexture;
                    this.MaskingColor = Color.Black * .75f;
                    this.GetRender = Render.Texture2D;
                }
                else if (Background != default)
                {
                    this.RenderTexture = Main.BaseTexture;
                    this.MaskingColor = Background;
                    this.GetRender = Render.Texture2D;
                }
                Invalidated += () => Reformat();
            }

            /// <summary>
            /// Adds the reformats the stack and uses the current Destination values as Padding.
            /// </summary>
            /// <param name="components"></param>
            public void AddRange(bool reformat, params InterfaceComponent[] components)
            {
                foreach (var c in components)
                {
                    if (c is null)
                        continue;
                    c.STACKPANEL_Padding = c.Destination.Location;
                    c.AddToParent(this);
                }
                if (reformat) Reformat();
            }

            /// <summary>
            /// Stacks each component visually.
            /// </summary>
            /// <param name="Font">If no components are SpriteFont components leave this default.</param>
            public void Reformat()
            {
                int heightPadding = -1, widthPadding = -1;
                int widestComponent = -1;
                Point _lastPoint = Point.Zero;
                foreach (var c in Components)
                {
                    _lastPoint.Y += c.STACKPANEL_Padding.Y;
                    c.Y = _lastPoint.Y;
                    c.HLock = HorizontalLock.Center;
                    if (heightPadding == -1)
                        heightPadding = c.STACKPANEL_Padding.Y;
                    if (widthPadding == -1)
                        widthPadding = c.STACKPANEL_Padding.X;
                    if (c.GetRender == Render.InterfaceFont)
                    {
                        var _temp = c.Font.Measure(c.RenderText).ToPoint();
                        c.Size = _temp;
                        _lastPoint.Y += _temp.Y;
                        if (widestComponent < _temp.X + (c.STACKPANEL_Padding.X * 2))
                            widestComponent = _temp.X + (c.STACKPANEL_Padding.X * 2);
                    }
                    else if (c.GetRender == Render.Texture2D)
                    {
                        _lastPoint.Y += c.Destination.Height;
                        if (widestComponent < c.Destination.Width + (c.STACKPANEL_Padding.X * 2))
                            widestComponent = c.Destination.Width + (c.STACKPANEL_Padding.X * 2);
                    }
                    if (c.Width == -1)
                        c.AutoSizeWidth = true;
                    if (c.Height == -1)
                        c.AutoSizeHeight = true;
                }
                STACKPANEL_Padding = new Point(widthPadding, heightPadding);
                if (Destination.Height == 0)
                    AutoSizeHeight = true;
                if (Destination.Width == 0)
                    AutoSizeWidth = true;
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                foreach (var c in Components)
                    c.Update(gameTime);
            }

            public override void Draw(SpriteBatch batch)
            {
                base.Draw(batch);             
                foreach(var c in Components)
                    c.Draw(batch);                
            }
        }        

        public Point UI_ScreenSize { get => Size; }

        public UserInterface(ContentManager content, Point Viewport)
        {
            InterfaceFont.LoadFonts();
            Size = Viewport;
        }

        public StackPanel Notification;
        public bool IsNotificationOpen { get => Notification != null; }
        TimeSpan _notifyTimer = TimeSpan.Zero;
        TimeSpan _timeSinceOpened = TimeSpan.Zero;
        bool _timeBasedNotification = false;
        public void ShowNotification(string text, Color Background, Color Foreground, TimeSpan OpenFor = default)
        {
            if (IsNotificationOpen)
                HideNotification();
            Notification = new StackPanel(Background, false);
            Notification.HLock = HorizontalLock.Center;
            Notification.VLock = VerticalLock.Bottom;
            Notification.AddRange(true, new InterfaceComponent().CreateText(new InterfaceFont(14, InterfaceFont.Styles.Bold), text, Foreground, new Point(30, 20)));
            Notification.AddToParent(this);
            _timeSinceOpened = TimeSpan.Zero;
            _notifyTimer = OpenFor;
            if (OpenFor != default)
                _timeBasedNotification = true;
            else
                _timeBasedNotification = false;
        }
        public void HideNotification()
        {
            if (Notification is null)
                return;
            Components.Remove(Notification);
        }

        public new void Update(GameTime gameTime)
        {
            var Viewport = UI_ScreenSize;
            try
            {
                foreach (var c in Components)
                    c.Update(gameTime);
            }
            catch { }
            if (IsNotificationOpen && _timeBasedNotification)
            {
                _timeSinceOpened += gameTime.ElapsedGameTime;
                if (_timeSinceOpened >= _notifyTimer)
                    HideNotification();
            }
            CleanComponents();
        }

        public new void Draw(SpriteBatch sprite)
        {
            try
            {
                if (Components.Where(x => x.Exclusive).Any())//game paused
                    sprite.Draw(Main.BaseTexture, new Rectangle(0, 0, UI_ScreenSize.X, UI_ScreenSize.Y), Color.Black * .5f);
                foreach (var c in Components)
                {
                    c.Draw(sprite);
                }
            }
            catch
            {

            }
        }
    }
}
