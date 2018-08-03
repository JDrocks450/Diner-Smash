using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diner_Smash
{
    public class UserInterface
    {
        public class ObjectSpawnList : InterfaceComponent
        {
            public ContentManager Content;
            public StackPanel Formatter;
            public bool IsVisible = false;

            public ObjectSpawnList(ContentManager Content, Point Location)
            {
                this.Content = Content;
                Formatter = new StackPanel();
                var objsList = new UserInterface.InterfaceComponent[Enum.GetNames(typeof(GameObject.ObjectNameTable)).Count() + 1];
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
                Formatter.AddRange(true, objsList);
                Formatter.Location = Location;
                Main.GlobalInput.UserInput += GlobalInput_UserInput;
            }

            private void GlobalInput_UserInput(InputHelper.InputEventArgs e)
            {                
                if (e.PressedKeys.Contains(Keys.F2))
                    IsVisible = !IsVisible;
            }

            public override void Update(GameTime gameTime)
            {
                if (!IsVisible)
                    return;
                foreach (var i in Formatter.Components)
                    i.Update(gameTime);
            }

            private void ObjectSpawnList_OnClick(Button sender)
            {
                var enumval = (ObjectContext.ObjectNameTable)sender.Tag;
                var value = GameObject.Create("object", enumval, Content);
                value.Load(Content);
                Main.AddObject(value);
            }

            public override void Draw(SpriteBatch batch, SpriteFont Font)
            {
                if (IsVisible)
                    Formatter.Draw(batch, Font);
            }
        }

        public class TextBox : UserInterface.InterfaceComponent
        {
            public delegate void OnTextChangedEventHandler(TextBox sender, string NewText, string Changes);
            public event OnTextChangedEventHandler TextChanged;

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
                CreateButton("Untitled", Color.Black * .75f, Color.White, Color.Gray * .75f, Color.Gray,
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
                Main.GlobalInput.UserInput += GlobalInput_UserInput;
                GetRender = Render.Texture2D;
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

            TimeSpan _timeSinceLastHold;
            bool canHold;
            public override void Update(GameTime gameTime)
            {
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
                    if (_timeSinceLastHold.TotalSeconds > .05f)
                    {
                        canHold = true;
                        _timeSinceLastHold = TimeSpan.Zero;
                    }
                }
                base.Update(gameTime);
            }

            public override void Draw(SpriteBatch sprite, SpriteFont Font)
            {
                Color DrawColor = Background;
                if (IsMouseOver && Mouse.GetState().LeftButton != ButtonState.Pressed) //Hover
                    DrawColor = High;
                if (IsMouseOver && Mouse.GetState().LeftButton == ButtonState.Pressed) //Hover
                    DrawColor = High;
                if (IsActive) //Active
                    DrawColor = Active;
                sprite.Draw(Main.BaseTexture, Destination, DrawColor);
                var cursorloc = Font.MeasureString(RenderText.Substring(0, CursorPosition)).ToPoint();
                var textSize = Font.MeasureString(RenderText);
                sprite.DrawString(Font, RenderText,
                    new Vector2(Destination.X + (int)(Destination.Height - textSize.Y),
                    Destination.Y + (Destination.Height / 2) - (int)(textSize.Y / 2)),
                    Foreground);
                sprite.Draw(Main.BaseTexture, new Rectangle(Destination.X + (Destination.Height - (int)textSize.Y) + cursorloc.X,
                    Destination.Y + (Destination.Height / 2) - (int)(textSize.Y / 2), 2, (int)textSize.Y), Foreground); //Draw cursor
            }
        }

        public class Button : UserInterface.InterfaceComponent
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
                if (e.MouseLeftClick)
                {
                    if (IsMouseOver)
                    {
                        OnClick?.Invoke(this);
                        IsMouseOver = false;
                    }
                }
            }

            public override void Update(GameTime gameTime)
            {
                var mouse = Mouse.GetState();
                var MouseRect = new Rectangle(mouse.Position, new Point(1, 1));
                IsMouseOver = false;
                if (MouseRect.Intersects(Destination))
                    IsMouseOver = true;
                base.Update(gameTime);
            }

            public override void Draw(SpriteBatch sprite, SpriteFont Font)
            {
                Color DrawColor = Background;
                if (IsMouseOver && Mouse.GetState().LeftButton != ButtonState.Pressed) //Hover
                    DrawColor = High;
                if (IsMouseOver && Mouse.GetState().LeftButton == ButtonState.Pressed) //Hover
                    DrawColor = Click;
                sprite.Draw(Main.BaseTexture, Destination, DrawColor);
                var textSize = Font.MeasureString(RenderText);
                sprite.DrawString(Font, RenderText,
                    new Vector2(Destination.X + (Destination.Width / 2) - (int)(textSize.X / 2),
                    Destination.Y + (Destination.Height / 2) - (int)(textSize.Y / 2)), 
                    Foreground);
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

            public UserInterface.InterfaceComponent[] Format()
            {
                return new InterfaceComponent[]
                {
                    new InterfaceComponent().CreateText($"FPS: {CurrentFramesPerSecond}", CurrentFramesPerSecond > 30 ? Color.Green : Color.Red, new Point(10)),
                    new InterfaceComponent().CreateText($"Total Frames: {TotalFrames}", Color.White, new Point(10, 5)),
                    new InterfaceComponent().CreateText($"Average FPS: {AverageFramesPerSecond}", Color.White, new Point(10, 5)),
                    new InterfaceComponent().CreateText(string.Format("Game Time: {0:hh\\:mm\\:ss}", TotalGameTime), Color.White, new Point(10, 5))
                };
            }
        }

        public class StackPanel : InterfaceComponent
        {
            public List<InterfaceComponent> Components = new List<InterfaceComponent>();

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
                Invalidated += () => Reformat(Main.UILayer.Font);
            }

            /// <summary>
            /// Adds the reformats the stack and uses the current Destination values as Padding.
            /// </summary>
            /// <param name="components"></param>
            public void AddRange(bool reformat, params InterfaceComponent[] components)
            {
                foreach (var c in components)
                {
                    c.STACKPANEL_Padding = c.Destination.Location;
                    c.Parent = this;
                }
                Components.AddRange(components);
                if (reformat) Reformat(Main.UILayer.Font);
            }

            /// <summary>
            /// Stacks each component visually.
            /// </summary>
            /// <param name="Font">If no components are SpriteFont components leave this default.</param>
            public void Reformat(SpriteFont Font = default)
            {
                int heightPadding = -1, widthPadding = -1;
                int widestComponent = -1;
                Point _lastPoint = Destination.Location;
                foreach (var c in Components)
                {
                    _lastPoint.Y += c.STACKPANEL_Padding.Y;
                    c.Location = new Point(_lastPoint.X = Destination.X + c.STACKPANEL_Padding.X, _lastPoint.Y);
                    if (heightPadding == -1)
                        heightPadding = c.STACKPANEL_Padding.Y;
                    if (widthPadding == -1)
                        widthPadding = c.STACKPANEL_Padding.X;
                    if (c.GetRender == Render.SpriteFont)
                    {
                        var _temp = Font.MeasureString(c.RenderText).ToPoint();
                        c.Size = _temp;
                        _lastPoint.Y += _temp.Y;
                        if (widestComponent < _temp.X + c.STACKPANEL_Padding.X)
                            widestComponent = _temp.X + c.STACKPANEL_Padding.X;
                    }
                    else if (c.GetRender == Render.Texture2D)
                    {
                        _lastPoint.Y += c.Destination.Height;
                        if (widestComponent < c.Destination.Width + c.STACKPANEL_Padding.X)
                            widestComponent = c.Destination.Width + c.STACKPANEL_Padding.X;
                    }
                }
                if (Destination.Height == 0)
                    AutoSizeHeight = true;
                if (AutoSizeHeight)
                    Height = heightPadding + _lastPoint.Y - Destination.Y;
                if (Destination.Width == 0)
                    AutoSizeWidth = true;
                if (AutoSizeWidth)
                    Width = widthPadding + widestComponent;
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                foreach (var c in Components)
                    c.Update(gameTime);
            }

            public override void Draw(SpriteBatch batch, SpriteFont font = default)
            {
                base.Draw(batch, default);             
                foreach(var c in Components)
                    c.Draw(batch, Main.UILayer.Font);                
            }
        }

        public class InterfaceComponent
        {
            public delegate void InvalidatedHandler();
            public event InvalidatedHandler Invalidated;

            public bool CenterScreen;
            public StackPanel Parent;
            public bool Exclusive { get; set; }
            public bool AutoSizeWidth;
            public bool AutoSizeHeight;
            public enum Render
            {
                Texture2D,
                SpriteFont,
                None
            }
            public Texture2D RenderTexture;
            public Rectangle Destination
            {
                get => new Rectangle(Location, Size);
                set
                {
                    Location = value.Location;
                    Size = value.Size;
                    Invalidated?.Invoke();
                }
            }

            public Point Location
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
            public int X;
            public int Y;
            public Point STACKPANEL_Padding;
            public string RenderText;
            public float TextSize;
            public Color MaskingColor = Color.White;
            public Render GetRender
            {
                get;
                internal set;
            } = Render.None;
            public object Tag;

            public InterfaceComponent CreateText(string Text, Color MaskingColor, Point Location)
            {
                RenderText = Text;
                this.MaskingColor = MaskingColor;
                Destination = new Rectangle(Location, Point.Zero);
                GetRender = Render.SpriteFont;
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

            public virtual void Update(GameTime gameTime)
            {
                if (Parent == null && CenterScreen)                
                    Location = new Point(UI_ScreenSize.X / 2 - Size.X / 2, UI_ScreenSize.Y / 2 - Size.Y / 2);                
            }

            public virtual void Draw(SpriteBatch sprite, SpriteFont Font)
            {
                switch (GetRender)
                {
                    case InterfaceComponent.Render.SpriteFont:
                        sprite.DrawString(Font, RenderText, Destination.Location.ToVector2(), MaskingColor);
                        break;
                    case InterfaceComponent.Render.Texture2D:
                        sprite.Draw(RenderTexture, Destination, MaskingColor);
                        break;
                }
            }
        }
        public SpriteFont Font;
        public List<InterfaceComponent> Components = new List<InterfaceComponent>();
        public static Point UI_ScreenSize;

        public UserInterface(ContentManager content, Point Viewport)
        {
            Font = content.Load<SpriteFont>("Font");
            UI_ScreenSize = Viewport;
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
            Notification.AddRange(true, new InterfaceComponent().CreateText(text, Foreground, new Point(30, 20)));
            Components.Add(Notification);
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

        public void Update(GameTime gameTime)
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
            if (IsNotificationOpen)
            {
                Notification.Location = 
                    new Point(Viewport.X / 2 - Notification.Destination.Width / 2, Viewport.Y - Notification.Destination.Height);
            }
        }

        public void Draw(SpriteBatch sprite)
        {
            try
            {
                if (Components.Where(x => x.Exclusive).Any())//game paused
                    sprite.Draw(Main.BaseTexture, new Rectangle(0, 0, UI_ScreenSize.X, UI_ScreenSize.Y), Color.Black * .5f);
                foreach (var c in Components)
                {
                    c.Draw(sprite, Font);
                }
            }
            catch
            {

            }
        }
    }
}
