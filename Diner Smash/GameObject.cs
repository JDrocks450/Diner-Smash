using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Diner_Smash.GameObject;
using static Diner_Smash.PathHelper;

namespace Diner_Smash
{        
    [Serializable]
    /// <summary>
    /// Holds the data for an object or a part of an object.
    /// </summary>
    public class ObjectContext
    {
        public ObjectContext(string Name = "Untitled", ObjectNameTable Identity = ObjectNameTable.None)
        {
            this.Name = Name;
            this.Identity = Identity;
        }

        public virtual void Init(Vector2 StartPosition, Point Size, Texture2D texture, float DrawIndex)
        {
            Location = StartPosition;
            Width = Size.X;
            Height = Size.Y;
            Texture = texture;
        }

        //public static float GetDrawIndex(Point Location)
        //{

        //}        

        public bool IsMouseOver
        {
            get;
            internal set;
        } = false;

        public enum ObjectNameTable { None, Table, Food, WaitHere, WelcomeMat, Player, Person, Menu, POS, CardboardBoxDesk };        
        public ObjectNameTable Identity;

        public bool IsRoutable = true;
        public bool IsInteractable = true;
        public bool IsClickable = true;
        public bool Interacting = false;

        public Color Mask = Color.White;
        public float Rotation = 0f;
        /// <summary>
        /// If left default, the rotation origin will be the center of the object.
        /// </summary>
        public Vector2 RotateOrigin = new Vector2(-1, -1);

        public string Name;
        public int ID;
        public float X;
        public float Y;
        /// <summary>
        /// The Object's width WITHOUT Scale factored in.
        /// </summary>
        public int Width;
        /// <summary>
        /// The Object's height WITHOUT Scale factored in.
        /// </summary>
        public int Height;
        public double Scale = 1;

        public enum AvailablityStates
        {
            Enabled,
            Invisible,
            Disabled
        }
        public AvailablityStates Availablity = AvailablityStates.Enabled;

        [NonSerialized]
        public Texture2D Texture;
        [NonSerialized]
        public SpriteEffects Effects;

        /// <summary>
        /// 0 --> 1; Closer to 1 it is, the farther back it is drawn.
        /// </summary>
        public float DrawIndex
        {
            get; set;
        }

        public Vector2 Location
        {
            set
            {
                X = value.X;
                Y = value.Y;
            }
            get => new Vector2(X, Y);
        }

        public Point Size
        {
            set
            {
                Width = value.X;
                Height = value.Y;
            }
            get => new Point(Width, Height);
        }

        /// <summary>
        /// Used for basic collision detection.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                return new Rectangle((int)X, (int)Y, (int)(Width * Scale), (int)(Height * Scale));
            }
            set
            {
                X = value.X;
                Y = value.Y;
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        /// Overridden to load texture during Main.Load();
        /// </summary>
        /// <param name="Content"></param>
        public virtual void Load(ContentManager Content = default)
        {
            if (Texture != null)
            {
                if (Width == 0)
                    Width = Texture.Width;
                if (Height == 0)
                    Height = Texture.Height;
            }
        }
    }

    [Serializable]
    public class GameObject : ObjectContext
    {
        public delegate void OnClickHandler(ObjectContext Affected);
        public event OnClickHandler OnClick;

        internal Color DEBUG_Highlight;
        /// <summary>
        /// The slot the character navigates to interact with the object.
        /// </summary>
        public virtual Point InteractionPoint
        {
            get => Location.ToPoint();
        }   

        /// <summary>
        /// Loads the Object
        /// </summary>
        /// <param name="Start"></param>
        /// <param name="Size"></param>
        public GameObject(string Name, ObjectNameTable Identity = ObjectNameTable.None) : base (Name, Identity)
        {
            OnClick += GameObject_OnClick;            
            Main.GlobalInput.UserInput += GlobalInput_UserInput;            
        }

        private void GlobalInput_UserInput(InputHelper.InputEventArgs e)
        {
            if (IsDragging)
                if (e.PressedKeys.Contains(Keys.Delete))
                {
                    Main.ObjectDragging = null;
                    Main.Objects.Remove(this);
                }
            if (e.MouseLeftClick && IsMouseOver && !Main.PlacementMode)
                Interact(Main.Player);
        }

        public bool IsDragging { get; private set; } = false;
        Point LastMousePos = Point.Zero;
        private void GameObject_OnClick(ObjectContext Affected)
        {            
            if ((!Main.PlacementMode && !Draggable) || IsDragging || Main.ObjectDragging != null)
                return;
            LastMousePos = Mouse.GetState().Position;
            IsDragging = true;
            Main.ObjectDragging = this;
        }

        public virtual void Update(GameTime gameTime)
        {
            if (Availablity != AvailablityStates.Enabled)
                return;
            if (IsClickable)
                VerifyMouseClick(Mouse.GetState());
            if (IsDragging)
            {                
                Point change = Mouse.GetState().Position - LastMousePos;
                if (Mouse.GetState().RightButton == ButtonState.Released)
                    Location += change.ToVector2();
                LastMousePos = Mouse.GetState().Position;
                if (Mouse.GetState().LeftButton == ButtonState.Released || (!Main.PlacementMode && !Draggable))
                {
                    IsDragging = false;
                    Main.ObjectDragging = null;
                }
            }
            if (Interacting)
                DEBUG_Highlight = Color.Orange;
            else
                DEBUG_Highlight = Color.Transparent;
        }

        /// <summary>
        /// Checks if the player is ready to interact with the object.
        /// </summary>
        /// <returns></returns>
        public virtual bool Interact(Player Focus, bool Force = false)
        {
            if (!IsInteractable)
                return false;
            Interacting = true;
            if (!Force)
                if (Focus.TranslateLocationtoNavNodeLocation() != InteractionPoint)
                {
                    Focus.Tasks.Add(new Task(() => Focus.RequestNavigation(InteractionPoint)));
                    Focus.Tasks.Add(new Task(() => Focus.RequestInteraction(ID)));
                    return false;
                }
                else                
                    return false;                
            return true;
        }

        public virtual string[] ReturnDebugInfo()
        {            
            return new string[] { $"*{Identity}", $"Location: {Location}", $"Size: {new Point(Width, Height)}",  $"MouseCollision: {_mouseLastCollision}", $"Mouse Location: {Main.MousePosition}" };
        }

        Point _mouseLastCollision;
        /// <summary>
        /// Checks through each context to see if the mouse is clicking it
        /// </summary>
        /// <param name="CheckThrough"></param>
        /// <param name="mouse"></param>
        /// <returns></returns>
        public bool VerifyMouseClick(MouseState mouse)
        {            
            IsMouseOver = false;
            var MouseRect = new Rectangle(Main.MousePosition, new Point(1, 1));
            var results = BoundingRectangle.Intersects(MouseRect);                        
            if (Main.GameCamera.Zoom != 1)
            {                
                return false;
            }            
            if (results) //Per-Pixel detection (fast)
            {
                _mouseLastCollision = (Main.MousePosition - BoundingRectangle.Location);
                var scalechange = Scale/1;
                var offsetX = (_mouseLastCollision.X * scalechange);
                var offsetY = (_mouseLastCollision.Y * scalechange);
                _mouseLastCollision.X = (int)offsetX;
                _mouseLastCollision.Y = (int)offsetY;
                var data = new Color[1];
                Texture.GetData(0, new Rectangle(_mouseLastCollision, new Point(1, 1)), data, 0, 1);
                if (data[0] != Color.Transparent)
                    IsMouseOver = true;
                if (IsMouseOver && Mouse.GetState().LeftButton == ButtonState.Pressed)
                    OnClick?.Invoke(this);
            }
            return results;
        }

        /// <summary>
        /// Writes vital information to an XML Element
        /// </summary>
        /// <param name="WriteTo"></param>
        public virtual void XmlSerialize(XElement WriteTo)
        {
            var e = WriteTo;
            if (e.Element("GameObjectIdentity") == null)
                e.Add(new XElement("GameObjectIdentity", Enum.GetName(typeof(ObjectNameTable), Identity)));
            e.Add(new XElement("Name", Name),
                new XElement("X", X),
                new XElement("Y", Y));
        }

        public virtual GameObject XmlDeserialize(XElement ReadFrom, ContentManager Content)
        {
            var e = ReadFrom;
            if (e.Element("GameObjectIdentity") == null)
                throw new Exception("Incorrect Format!");
            var name = e.Element("Name").Value;
            GameObject value = Create(name, (ObjectNameTable)Enum.Parse(typeof(ObjectNameTable), e.Element("GameObjectIdentity").Value), Content);          
            value.X = int.Parse(e.Element("X").Value);
            value.Y = int.Parse(e.Element("Y").Value);
            return value;
        }

        /// <summary>
        /// Creates, Loads, and returns a GameObject of the desired type.
        /// </summary>
        /// <param name="Name">The SaveFile name of the object.</param>
        /// <param name="Type">The type of object to spawn.</param>
        /// <param name="Content">The ContentManager to load the graphics.</param>
        /// <returns></returns>
        public static GameObject Create(string Name, ObjectNameTable Type, ContentManager Content)
        {
            var obj = new GameObject("");
            switch (Type)
            {
                case ObjectNameTable.CardboardBoxDesk:
                    obj = new Desk(Name);
                    break;
                case ObjectNameTable.WelcomeMat:
                    obj = new WelcomeMat(Name);
                    break;
                case ObjectNameTable.Table:
                    obj = new Table(Name, Main.Objects.Where(x => x.Identity == ObjectNameTable.Table).Count() + 1);
                    break;
                case ObjectNameTable.Person:
                    var i = Main.GlobalRandom.Next(0, Enum.GetNames(typeof(Person.PersonNameTable)).Length);
                    obj = new Person(Name, (Person.PersonNameTable)i);
                    break;
                case ObjectNameTable.Menu:
                    obj = new Menu(-1);
                    break;
                case ObjectNameTable.Food:
                    obj = new Food(-1);
                    break;
                case ObjectNameTable.POS:
                    obj = new POSObject(Name);
                    break;
                default:
                    return null;
            }
            obj.ID = Main.Objects.Count + 1;
            obj.Load(Content);
            return obj;
        }

        public bool Draggable
        {
            get;
            set;
        } = false;

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            var orig = RotateOrigin;
            if (Rotation != 0f && orig == new Vector2(-1, -1))
                orig = new Vector2(Width / 2, Height / 2);
            if (Availablity == AvailablityStates.Enabled && Texture != null)
                spriteBatch.Draw(Texture, BoundingRectangle, null, Mask, Rotation, orig, Effects, DrawIndex);
            if (Main.IsDebugMode)
            {
                if (IsClickable)
                    spriteBatch.Draw(Main.BaseTexture, new Rectangle(Location.ToPoint() + _mouseLastCollision, new Point(10)), Color.DeepSkyBlue);
                spriteBatch.Draw(Main.BaseTexture, BoundingRectangle, DEBUG_Highlight * .5f);
            }
        }
    }
}
