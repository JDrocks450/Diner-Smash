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
    [System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DinerSmashObjectOption : System.Attribute
    {

    }    

    /// <summary>
    /// Values specifying a Zindex that is within the reserved value range.
    /// DIVIDE EACH BY 100 TO GET FLOAT VALUE
    /// </summary>
    public enum ReservedZIndicies : short
    {
        ReservedValueRangeEnd = 10,
        Shadows = 5,
        FlatObjects = 4,
    }
    
    /// <summary>
    /// Holds the data for an object or a part of an object.
    /// </summary>
    public abstract class ObjectContext
    {
        public ObjectContext(string Name = "Untitled", ObjectNameTable Identity = ObjectNameTable.None)
        {
            this.Name = Name;
            this.Identity = Identity;
            if (Identity != ObjectNameTable.None)
            {
                Customizer = new UserInterface.ObjectCustomizer(this);
                Customizer.Availablity = AvailablityStates.Invisible;
            }
            ShadowHandler = new Lighting();
        }

        public virtual void Init(Vector2 StartPosition, Point Size, Texture2D texture, float DrawIndex)
        {
            Location = StartPosition;
            Width = Size.X;
            Height = Size.Y;
            Texture = texture;
        }

        public virtual float GetDrawIndex()
        {
            if (IsObjectFlat)
            {
                return ((float)ReservedZIndicies.FlatObjects / 100);
            }
            if (OVERRIDE_LAYER != -1)
                return OVERRIDE_LAYER;
            int scrHeight = Main.SourceLevel.LevelSize.Y; 
            var index = Location.Y / scrHeight;
            if (index < ((float)ReservedZIndicies.ReservedValueRangeEnd/100))
                index = ((float)ReservedZIndicies.ReservedValueRangeEnd / 100);
            return index;
        }        

        public bool IsMouseOver
        {
            get;
            internal set;
        } = false;

        public enum ObjectNameTable { None, Table, Food, WaitHere, WelcomeMat, Player, Person, Menu, POS, CardboardBoxDesk, FoodCounter };        
        public ObjectNameTable Identity;

        public bool HasPlacementSlots { get => PlacementSlots?.Count > 0; }
        /// <summary>
        /// Do not add points scaled to the object, the engine scales them for you in GetPlacementSlot()
        /// </summary>
        public Dictionary<Point, GameObject> PlacementSlots = new Dictionary<Point, GameObject>();
        /// <summary>
        /// Provides the PlacementSlot that has been scaled and positioned properly.
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public KeyValuePair<Point, GameObject> GetPlacementSlot(int Index)
        {
            var r = PlacementSlots.ElementAt(Index);
            return new KeyValuePair<Point, GameObject>(Location.ToPoint() + 
                (r.Key.ToVector2() * (float)Scale).ToPoint(), r.Value);
        }
        /// <summary>
        /// Attempts to place an object in the slot but will fail if the slot isn't empty.
        /// </summary>
        /// <param name="SlotIndex"></param>
        /// <param name="Object"></param>
        /// <returns></returns>
        public bool PlaceObjectInSlot(int SlotIndex, GameObject Object)
        {
            var r = PlacementSlots[PlacementSlots.ElementAt(SlotIndex).Key];
            if (r is null)
            {
                r = Object;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Attempts to place an object in the next available slot but will fail if no slots are open.
        /// </summary>
        /// <param name="SlotIndex"></param>
        /// <param name="Object"></param>
        /// <returns></returns>
        public bool PlaceObjectInSlot(GameObject Object)
        {
            var query = PlacementSlots.Where(x => x.Value is null);
            if (!query.Any())
                return false;
            var r = PlacementSlots[query.First().Key];
            if (r is null)
            {
                PlacementSlots[query.First().Key] = Object;
                return true;
            }
            return false;
        }
        public void RemoveObjectFromSlot(GameObject Object)
        {
            PlacementSlots[PlacementSlots.Where(x => x.Value == Object).First().Key] = null;
        }

        public UserInterface.ObjectCustomizer Customizer;
        public bool IsDragging { get; internal set; } = false;
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
        [DinerSmashObjectOption]
        public int ID;
        [DinerSmashObjectOption]
        public float X;
        [DinerSmashObjectOption]
        public float Y;
        [DinerSmashObjectOption]
        /// <summary>
        /// The Object's width WITHOUT Scale factored in.
        /// </summary>
        public int Width;
        [DinerSmashObjectOption]
        /// <summary>
        /// The Object's height WITHOUT Scale factored in.
        /// </summary>
        public int Height;
        [DinerSmashObjectOption]
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

        public bool HasShadow = true;
        public Lighting ShadowHandler;
        private float OVERRIDE_LAYER = -1;

        /// <summary>
        /// Sets the layer depth to the reserved flat objects value
        /// </summary>
        public bool IsObjectFlat { get; set; } = false;
        [DinerSmashObjectOption]
        /// <summary>
        /// 0 --> 1; Closer to 1 it is, the farther back it is drawn.
        /// </summary>
        public float LayerDepth
        {
            //Should be safe since GameObject.Draw requires a texture before getting DrawIndex            
            get => GetDrawIndex();
            set => OVERRIDE_LAYER = value;
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
            get => new Point((int)(Width * Scale), (int)(Height * Scale));
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
        /// The slot the character navigates to interact with the object.
        /// </summary>
        public virtual Point InteractionPoint
        {
            get => Location.ToPoint();
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
                if (HasShadow)
                    ShadowHandler.GenerateShadow(Texture);
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
            if (IsMouseOver)
            {
                if (e.PressedKeys.Contains(Keys.Delete))
                {
                    Dispose();
                }
                else if (e.PressedKeys.Contains(Keys.F))
                {
                    var i = (int)Effects;
                    i++;
                    if (i > 2)
                        i = 0;
                    Effects = (SpriteEffects)i;
                }
                if (e.PressedKeys.Contains(Keys.Up))
                    Scale += .05;
                else if (e.PressedKeys.Contains(Keys.Down))
                    Scale -= .05;
            }
            if (e.MouseLeftClick && IsMouseOver && !Main.PlacementMode)
                Interact(Player.ControlledCharacter);
        }               

        /// <summary>
        /// Provides a safe way to delete the object from the game world.
        /// </summary>
        public void Dispose()
        {
            Customizer?.RemoveFromParent();
            foreach (var i in GetType().GetFields())
                try
                {
                    i.SetValue(this, default);
                }
                catch { }
            Main.ObjectDragging = null;
            Main.Objects.Remove(this);            
        }

        public Point LastMousePos = Point.Zero;
        public Vector2 ScaledCollisionMousePosition;
        private void GameObject_OnClick(ObjectContext Affected)
        {            
            if ((!Main.PlacementMode && !Draggable) || IsDragging || Main.ObjectDragging != null)
                return;
            LastMousePos = Main.MousePosition.ToPoint();
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
                Point change = Main.MousePosition.ToPoint() - LastMousePos;
                if (Mouse.GetState().RightButton == ButtonState.Released)
                    Location += change.ToVector2();
                LastMousePos = Main.MousePosition.ToPoint();
                if (Mouse.GetState().LeftButton == ButtonState.Released || (!Main.PlacementMode && !Draggable))
                {
                    IsDragging = false;
                    Main.ObjectDragging = null;
                }
            }
            if (Customizer != null && Main.PlacementMode)
                if (IsMouseOver)
                    Customizer.Availablity = AvailablityStates.Enabled;
                else
                    Customizer.Availablity = AvailablityStates.Invisible;
            //Correct Object Position        
            {
                if (BoundingRectangle.Right > Main.SourceLevel.LevelSize.X)
                    X = Main.SourceLevel.LevelSize.X - Width;
                if (X < 0)
                    X = 0;
                if (BoundingRectangle.Bottom > Main.SourceLevel.LevelSize.Y)
                    X = Main.SourceLevel.LevelSize.Y - Height;
                if (Y < -BoundingRectangle.Center.Y)
                    Y = -BoundingRectangle.Center.Y;
            }
            //Correct PlacementSlot Positions
            {
                for (int i = 0; i < PlacementSlots.Count; i++)
                {
                    var slot = GetPlacementSlot(i);
                    if (slot.Value is null)
                        continue;
                    slot.Value.Location = new Vector2(
                        slot.Key.X - slot.Value.Size.X / 2,
                        slot.Key.Y - slot.Value.Size.Y);
                }
            }
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
            var l = new List<string>();
            foreach(var i in GetType().GetFields().Where(x => x.GetCustomAttributes(typeof(DinerSmashObjectOption), true).Any()))
            {
                l.Add($"{i.Name} = {i.GetValue(this)}");
            }
            foreach (var i in GetType().GetProperties().Where(x => x.GetCustomAttributes(typeof(DinerSmashObjectOption), true).Any()))
            {
                l.Add($"{i.Name} = {i.GetValue(this)}");
            }
            return l.ToArray();
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
                new XElement("Y", Y),
                new XElement("spriteEffect", (int)Effects));
        }

        public virtual GameObject XmlDeserialize(XElement ReadFrom, ContentManager Content)
        {            
            var e = ReadFrom;
            if (e.Element("GameObjectIdentity") == null)
                throw new Exception("Incorrect Format!");
            var name = e.Element("Name").Value;
            GameObject value = Create(name, (ObjectNameTable)Enum.Parse(typeof(ObjectNameTable), e.Element("GameObjectIdentity").Value));         
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
        public static GameObject Create(string Name, ObjectNameTable Type)
        {
            var Content = Main.Manager;
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
            {
                spriteBatch.Draw(Texture, BoundingRectangle, null,
                    Color.Lerp(Mask, Lighting.LightColor, Lighting.LightIntensity),
                    Rotation, orig, Effects, LayerDepth);
                ShadowHandler.CastShadow(spriteBatch, Location, Scale);
                if (HasPlacementSlots)
                    for (int i = 0; i < PlacementSlots.Count; i++)
                    {
                        var o = GetPlacementSlot(i).Value;
                        if (o != null)
                        {
                            o.LayerDepth = LayerDepth + .01f;
                            o.Rotation = Rotation;
                            o.Draw(spriteBatch);
                        }
                    }
            }
            if (Main.IsDebugMode)
            {
                if (IsClickable)                
                    spriteBatch.Draw(Main.BaseTexture, Location + ScaledCollisionMousePosition, null, IsMouseOver ? Color.Green : Color.Red,0f, Vector2.Zero, 10, SpriteEffects.None, 1);
                if (HasPlacementSlots)
                    for (int i = 0; i < PlacementSlots.Count; i++)
                    {
                        var s = GetPlacementSlot(i);
                        spriteBatch.Draw(Main.BaseTexture, s.Key.ToVector2(), null, s.Value != null ? Color.DeepSkyBlue : Color.Blue, 0f, Vector2.Zero, 10, SpriteEffects.None, 1);
                    }
                if (Main.DEBUG_HighlightingMode)
                    DEBUG_Highlight = Color.Green;
                spriteBatch.Draw(Main.BaseTexture, BoundingRectangle, null, DEBUG_Highlight * .5f, 0f, Vector2.Zero, Effects, 1);
            }
        }
    }
}
