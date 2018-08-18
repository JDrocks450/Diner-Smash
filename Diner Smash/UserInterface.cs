using Microsoft.Xna.Framework;
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

        public InterfaceComponent CreateText(string Text, Color MaskingColor, Point Location = default, bool ChangeLocation = true, HorizontalLock Align = HorizontalLock.Center)
        {
            RenderText = Text;
            this.MaskingColor = MaskingColor;
            if (ChangeLocation)
                Destination = new Rectangle(Location, Point.Zero);
            GetRender = Render.InterfaceFont;
            HLock = Align;
            return this;
        }

        public InterfaceComponent CreateText(InterfaceFont Font, string Text, Color MaskingColor, Point Location = default, HorizontalLock Align = HorizontalLock.Center)
        {
            RenderText = Text;
            this.MaskingColor = MaskingColor;
            Destination = new Rectangle(Location, Point.Zero);
            GetRender = Render.InterfaceFont;
            this.Font = Font;
            HLock = Align;
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

        public Button CreateButton(string Text, Color BackgroundColor, Color ForeColor, Color Highlight, Color Click, Rectangle Space, HorizontalLock Align = HorizontalLock.None)
        {
            return new Button().CreateButton(Text, BackgroundColor, ForeColor, Highlight, Click, Space, Align);
        }

        public TextBox CreateTextBox(string Text, Color BackgroundColor, Color ForeColor, Color Highlight, Color Click, Rectangle Space, HorizontalLock Align = HorizontalLock.None)
        {
            return new TextBox().CreateTextBox(Text, BackgroundColor, ForeColor, Highlight, Click, Space, Align);
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
            var newLoc = Point.Zero;
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
            newLoc += Margin + Parent.Destination.Location;
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

    public abstract class InterfaceParentComponent : InterfaceComponent
    {
        public List<InterfaceComponent> Components { get; set; } = new List<InterfaceComponent>();

        const int DIALOG_BUTTON_Width = 135;
        const int DIALOG_BUTTON_Height = 35;

        public bool IsDialog { get; internal set; }
        public string DIALOG_ButtonOK = "OK";
        public string DIALOG_ButtonCANCEL = "Cancel";
        public bool? DIALOG_Result = null;
        /// <summary>
        /// Formats a dialog with a title and OK/CANCEL buttons, allowing you to add as many components in between as you like.
        /// </summary>
        /// <param name="Title"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public InterfaceParentComponent CreateDialog(string Title, HorizontalLock UserAddedComponentsLock, bool styleComponents = false, params InterfaceComponent[] components)
        {
            int addedcontrols = 3;
            IsDialog = true;
            VLock = VerticalLock.Center;
            HLock = HorizontalLock.Center;
            AddRange(HorizontalLock.Center,
                new InterfaceComponent().CreateText(
                    new InterfaceFont(14, InterfaceFont.Styles.Bold),
                    Title, Color.White, new Point(20, 20), HorizontalLock.Center));
            AddRange(HorizontalLock.Left, components);
            if (styleComponents)
            foreach(var c in components)
                {
                    if (c is TextBox)
                        c.CreateTextBox(c.RenderText,
                            Color.White * .2f,
                            Color.White,
                            Color.White * .5f,
                            Color.White * .6f,
                            new Rectangle(10, 10, -1, -1), c.HLock);
                }
            AddRange(HorizontalLock.Center,
                new InterfaceComponent().CreateButton(DIALOG_ButtonOK,
                    Color.White * .2f,
                    Color.White,
                    Color.Green * .5f,
                    Color.Green * .8f,
                    new Rectangle(0, 10, DIALOG_BUTTON_Width, DIALOG_BUTTON_Height)),
                new InterfaceComponent().CreateButton(DIALOG_ButtonCANCEL,
                    Color.White * .2f,
                    Color.White,
                    Color.Red * .5f,
                    Color.Red * .8f,
                    new Rectangle(0, 5, DIALOG_BUTTON_Width, DIALOG_BUTTON_Height)));
            (Components[addedcontrols + components.Length - 2] as Button).OnClick += (Button Sender) => { DIALOG_Result = true; CloseDialog(); };
            (Components[addedcontrols + components.Length - 1] as Button).OnClick += (Button Sender) => { DIALOG_Result = false; CloseDialog(); };
            AddToParent(Main.UILayer);
            return this;
        }

        public virtual void AddRange(HorizontalLock LockItems = HorizontalLock.Left, params InterfaceComponent[] components)
        {
            foreach (var c in components)
            {
                if (c is null)
                    continue;
                c.STACKPANEL_Padding = c.Destination.Location;
                c.AddToParent(this);
            }            
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

        public bool DialogOpened { get; internal set; }

        /// <summary>
        /// Displays the control and it's contents as a thread-blocking dialog.
        /// </summary>
        public Task<bool?> ShowAsDialog()
        {
            if (DialogOpened)
                throw new InvalidOperationException("You can't open an opened dialog... that's illegal");
            return Task.Run(() =>
                {
                    IsDialog = true;
                    Exclusive = true;
                    DialogOpened = true;
                    AddToParent(Main.UILayer);
                    while (DialogOpened) { }
                    RemoveFromParent();
                    Exclusive = false;
                    return DIALOG_Result;
                });
        }

        /// <summary>
        /// Closes this component if ShowDialog was called prior.
        /// </summary>
        public void CloseDialog()
        {
            if (!IsDialog)
                return;
            DialogOpened = false;
        }
    }

    public class UserInterface : InterfaceParentComponent
    {
        public class ObjectCustomizer : StackPanel
        {

        }

        public class ObjectSpawnList : StackPanel
        {
            public ContentManager Content { get => Main.Manager; }

            public ObjectSpawnList()
            {
                int newButtons = 3; //the amount of extra components to add
                var objsList = new InterfaceComponent[Enum.GetNames(typeof(GameObject.ObjectNameTable)).Count() + newButtons];
                objsList[0] = new InterfaceComponent().CreateText("Add Object to Room", Color.White, new Point(15));
                objsList[1] = new InterfaceComponent().CreateButton("Change Floor Color",
                    Main.GameScene.FloorMask * .25f,
                    Color.White,
                    Main.GameScene.FloorMask * .5f,
                    Main.GameScene.FloorMask * .60f,
                    new Rectangle(new Point(10, 10), new Point(175, 50)));
                (objsList[1] as Button).OnClick += ShowFloorColorChangeDialog;
                objsList[2] = new InterfaceComponent().CreateButton("Change Room Size",
                    Color.Black * .3f,
                    Color.White,
                    Color.White * .5f,
                    Color.DeepSkyBlue,
                    new Rectangle(new Point(10, 10), new Point(175, 50)));
                (objsList[2] as Button).OnClick += ShowChangeFloorSizeDialog; ;
                foreach (var s in Enum.GetNames(typeof(GameObject.ObjectNameTable)).Where(x => x != "None"))
                {
                    objsList[newButtons] = new InterfaceComponent().CreateButton(s,
                        Color.Black * .3f,
                        Color.White,
                        Color.White * .5f,
                        Color.DeepSkyBlue,
                        new Rectangle(newButtons == 1 ? new Point(10) : new Point(10, 2), new Point(175, 50)));
                    objsList[newButtons].Tag = Enum.Parse(typeof(GameObject.ObjectNameTable), s);
                    (objsList[newButtons] as Button).OnClick += ObjectSpawnList_OnClick;
                    newButtons++;
                }
                AddRange(HorizontalLock.Center, objsList);
                HLock = HorizontalLock.Right;
                Main.GlobalInput.UserInput += GlobalInput_UserInput;
                AddToParent(Main.UILayer);
            }

            private void ShowChangeFloorSizeDialog(Button sender)
            {
                new StackPanel(Color.Green * .5f, false).CreateDialog("Input a New Size for Your Room", 
                    HorizontalLock.Left, false,
                    new InterfaceComponent().CreateTextBox("Width", Color.White * .2f,
                    Color.White,
                    Color.White * .5f,
                    Color.White * .8f, new Rectangle(10, 5, 200, 50)),
                new InterfaceComponent().CreateTextBox("Height", Color.White * .2f,
                    Color.White,
                    Color.White * .5f,
                    Color.White * .8f, new Rectangle(5, 5, 200, 50))).ShowAsDialog(); //<-- Shows Dialog
            }

            private void ShowFloorColorChangeDialog(Button Sender)
            {
                using (System.Windows.Forms.ColorDialog d = new System.Windows.Forms.ColorDialog())
                {
                    d.Color = Main.GameScene.FloorMask.ToDrawingColor();
                    d.AnyColor = true;
                    d.AllowFullOpen = true;
                    d.FullOpen = true;
                    d.SolidColorOnly = false;
                    if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        Main.GameScene.FloorMask = d.Color.ToXNAColor();
                }
                Sender.Background = Main.GameScene.FloorMask * .25f;
                Sender.High = Main.GameScene.FloorMask * .5f;
                Sender.Click = Main.GameScene.FloorMask * .60f;
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
                try
                {
                    value.Location = Main.GameCamera.DesiredPosition - (value.Size / new Point(2)).ToVector2();
                }
                catch (NullReferenceException e) { System.Windows.Forms.MessageBox.Show("That Object has not been implemented yet!"); }
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

            public new TextBox CreateTextBox(string Text, Color BackgroundColor, Color ForeColor, Color Highlight, Color Active, Rectangle Space, HorizontalLock Align = HorizontalLock.None)
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
                HLock = Align;
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
                if (IsActive && e.PressedKeys.Any() && !FLAG_ClearKeyboard)
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
            bool FLAG_ClearKeyboard;

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
                var keyboard = Keyboard.GetState();
                if (canHold && IsActive && !FLAG_ClearKeyboard)
                {
                    UpperCase = keyboard.CapsLock;
                    foreach (var k in keyboard.GetPressedKeys())
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
                if (IsActive && !FLAG_ClearKeyboard)
                {
                    if ((keyboard.IsKeyDown(Keys.V) &&
                        (keyboard.IsKeyDown(Keys.LeftControl) ||
                        keyboard.IsKeyDown(Keys.RightControl))) &&
                        keyboard.GetPressedKeys().Count() == 2) //Check if only Ctrl and V are pressed (Left or Right Ctrl)
                    {
                        string str = "";
                        var t = new System.Threading.Thread(new System.Threading.ThreadStart(() => { str = GetClipboardText(); }));
                        t.SetApartmentState(System.Threading.ApartmentState.STA);
                        t.Start();
                        while (t.IsAlive) { }
                        RenderText = RenderText.Insert(CursorPosition, str);
                        CursorPosition = RenderText.Length;
                        FLAG_ClearKeyboard = true;
                    }
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

            [STAThread]
            static string GetClipboardText()
            {
                if (System.Windows.Forms.Clipboard.ContainsText())
                    return System.Windows.Forms.Clipboard.GetText();
                return "";
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

            public new Button CreateButton(string Text, Color BackgroundColor, Color ForeColor, Color Highlight, Color Click, Rectangle Space, HorizontalLock Align)
            {
                RenderText = Text;
                Background = BackgroundColor;
                Foreground = ForeColor;
                High = Highlight;
                this.Click = Click;
                Destination = Space;
                Main.GlobalInput.UserInput += GlobalInput_UserInput;
                GetRender = Render.Texture2D;
                HLock = Align;
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
            public override void AddRange(HorizontalLock LockItems = HorizontalLock.Left, params InterfaceComponent[] components)
            {
                base.AddRange(LockItems, components);
                Reformat(LockItems);
            }

            /// <summary>
            /// Stacks each component visually.
            /// </summary>
            /// <param name="Font">If no components are SpriteFont components leave this default.</param>
            public void Reformat(HorizontalLock LockItems = HorizontalLock.None)
            {
                int heightPadding = -1, widthPadding = -1;
                int widestComponent = -1;
                Point _lastPoint = Point.Zero;
                foreach (var c in Components)
                {
                    _lastPoint.Y += c.STACKPANEL_Padding.Y;
                    if (LockItems != HorizontalLock.None && c.HLock == HorizontalLock.None)
                        c.HLock = LockItems;
                    if (c.HLock != HorizontalLock.Center && c.Margin.X == 0)
                        c.Margin = new Point(c.STACKPANEL_Padding.X,0);
                    c.Y = _lastPoint.Y;
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
            Notification.AddRange(HorizontalLock.Center, new InterfaceComponent().CreateText(new InterfaceFont(14, InterfaceFont.Styles.Bold), text, Foreground, new Point(30, 20)));
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
