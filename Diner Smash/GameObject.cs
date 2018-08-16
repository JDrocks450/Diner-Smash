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

        public enum ObjectNameTable { None, Table, Food, WaitHere, WelcomeMat, Player, Person, Menu, POS, CardboardBoxDesk, FoodCounter };        
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
                else if (e.PressedKeys.Contains(Keys.F))
                {
                    var i = (int)Effects;
                    i++;
                    if (i > 2)
                        i = 0;
                    Effects = (SpriteEffects)i;
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

        public void Click()
        {
            if (IsMouseOver && IsClickable)
                OnClick?.Invoke(this);
        }

        public virtual void Update(GameTime gameTime)
        {
            if (Availablity != AvailablityStates.Enabled)
                return;
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
            else if (Main.DEBUG_HighlightingMode)
                DEBUG_Highlight = Color.Green;
            else DEBUG_Highlight = Color.Transparent;
        }

        /// <summary>
        /// Checks if the player is ready to interact with the object.
        /// </summary>
        /// <returns></returns>
        public virtual bool Interact(Player Focus, bool Force = false)
        {
            if (Focus is null)
                return false;
            if (Main.PlacementMode)
                return false;
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

        public Point _mouseLastCollision;        

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
                new XElement("Y", Y),
                new XElement("spriteEffect", (int)Effects));
        }

        public virtual GameObject XmlDeserialize(XElement ReadFrom, ContentManager Content)
        {
            var e = ReadFrom;
            if (e.Element("GameObjectIdentity") == null)
                throw new Exception("Incorrect Format!");
            var name = e.Element("Name").Value;
            GameObject value = Create(name, (ObjectNameTable)Enum.Parse(typeof(ObjectNameTable), e.Element("GameObjectIdentity").Value), Content);          
            value.X = float.Parse(e.Element("X").Value);
            value.Y = float.Parse(e.Element("Y").Value);
            value.Effects = (SpriteEffects)int.Parse(e.Element("spriteEffect").Value);
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
                case ObjectNameTable.Player:
                    obj = new Player(Name, (Main.Objects.Where(x => x is Player)?.Count() ?? 0) + 1);
                    break;
                case ObjectNameTable.CardboardBoxDesk:
                    obj = new Desk(Name);
                    break;
                case ObjectNameTable.WelcomeMat:
                    obj = new WelcomeMat(Name);
                    break;
                case ObjectNameTable.Table:
                    obj = new Table(Name, Main.Objects.Where(x => x.Identity == ObjectNameTable.Table)?.Count() ?? 0 + 1);
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
                case ObjectNameTable.FoodCounter:
                    obj = new FoodCounterObject(Name);
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
