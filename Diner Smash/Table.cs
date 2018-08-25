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
    public class Desk : GameObject
    {
        public Desk(string Name) : base(Name, ObjectNameTable.CardboardBoxDesk)
        {

        }

        public override void Load(ContentManager Content = null)
        {
            Texture = Content.Load<Texture2D>("Objects/CTable");
            base.Load(Content);            
        }        
    }
    public class Table : GameObject
    {
        Texture2D Tabletop, Tableseat;
        static Color[] BonusColors = new Color[4] { Color.MediumVioletRed, Color.DeepSkyBlue, Color.Green, Color.DarkRed };
        Color BonusColor = BonusColors[0];

        public int TableID;

        /// <summary>
        /// Defines every state the table can be in.
        /// </summary>
        public enum States
        {
            /// <summary>
            /// Nobody seated here
            /// </summary>
            Empty,
            /// <summary>
            /// Menus recieved
            /// </summary>
            Menus,
            /// <summary>
            /// Ready to order
            /// </summary>
            M_Ready,
            /// <summary>
            /// Waiting for food
            /// </summary>
            F_Waiting,
            /// <summary>
            /// Eating food
            /// </summary>
            Eating,
            /// <summary>
            /// Waiting for check
            /// </summary>
            C_Waiting,
            /// <summary>
            /// Check recieved
            /// </summary>
            Check,
            /// <summary>
            /// Trash on table
            /// </summary>
            Trash
        }
        public States TableState = States.Empty;

        public bool Occupied
        {
            get => TableState != States.Empty;
        }

        public override Point InteractionPoint
        {
            get
            {
                return new Point(BoundingRectangle.Center.X, BoundingRectangle.Bottom + Player.ControlledCharacter.PathFinder.Height);
            }
        }

        /// <summary>
        /// Represents items such as menus being placed on top of the table.
        /// </summary>
        public GameObject[] TabletopItems = new GameObject[4];
        public bool[] OccupiedSeats = new bool[4] { true,true,true,true };
        public Person[] People = new Person[4];

        public Table(string Name, int ID) : base(Name, ObjectNameTable.Table)
        {
            TableID = ID;           
        }

        public override void Load(ContentManager Content)
        {
            Tabletop = Content.Load<Texture2D>("Objects/Table/PART_Tabletop");
            Tableseat = Content.Load<Texture2D>("Objects/Table/PART_TableSeat");
            Texture = Tabletop;
            base.Load(Content);
        }

        TimeSpan TimeSinceMenus;
        TimeSpan TimeSinceFood;
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (IsMouseOver && Main.ObjectDragging is Person)
            {
                var r = PositionDraggedPeople();
                if (Mouse.GetState().LeftButton == ButtonState.Released && r)
                    SubmitSeatRequest(Main.ObjectDragging as Person);
            }
            PositionTableItems();
            DEBUG_Highlight = Color.Transparent;
            switch (TableState)
            {
                case States.Menus:
                    DEBUG_Highlight = Color.Blue;
                    TimeSinceMenus += gameTime.ElapsedGameTime;
                    if (TimeSinceMenus.TotalSeconds >= People[0].MENUS_Time)
                        TableState = States.M_Ready;
                    break;
                case States.M_Ready:
                    DEBUG_Highlight = Color.SeaGreen;
                    break;
                case States.F_Waiting:
                    DEBUG_Highlight = Color.Green;
                    break;
                case States.Eating:
                    DEBUG_Highlight = Color.DarkGreen;
                    TimeSinceFood += gameTime.ElapsedGameTime;
                    if (TimeSinceFood.TotalSeconds >= People[0].FOOD_EAT_Time)
                        TableState = States.C_Waiting;
                    break;
                case States.C_Waiting:
                    DEBUG_Highlight = Color.Magenta;
                    break;
            }
        }

        private void PositionTableItems()
        {
            ObjectNameTable identity = ObjectNameTable.None;
            switch (TableState)
            {
                case States.Menus:
                case States.M_Ready:
                    identity = ObjectNameTable.Menu;
                    break;                    
                case States.Eating:
                case States.C_Waiting:
                    identity = ObjectNameTable.Food;
                    break;
            }
            int i = 0;
            foreach (var seat in OccupiedSeats)
            {
                if (identity == ObjectNameTable.None)
                {
                    TabletopItems[i] = null;
                    i++;
                    continue;
                }
                if (!seat)
                {
                    TabletopItems[i] = null;
                    i++;
                    continue;
                }
                var loc = new Vector2();
                bool right = false, bottom = false;
                if (i == 1)
                    right = true;
                else if (i == 2)
                    bottom = true;
                else if (i == 3)
                {
                    right = true;
                    bottom = true;
                }
                var rotation = 0f;
                var incH = Height / 4;
                var incW = Width / 4;
                if (!bottom) //Top Half of Table           
                    loc.Y = (Location.Y + incH);
                else
                    loc.Y = (Location.Y + (incH * 3));
                if (!right) //Left Half of Table
                {
                    loc.X = (Location.X + incW);
                    loc.X += incW/3;
                    rotation = MathHelper.ToRadians(90);
                }
                else
                {
                    loc.X = (Location.X + (incW * 3));
                    loc.X -= incW / 3;
                    rotation = MathHelper.ToRadians(-90);
                }
                TabletopItems[i] = GameObject.Create("tableSpawn", identity);
                TabletopItems[i].Location = loc;
                if (identity == ObjectNameTable.Menu)
                    (TabletopItems[i] as Menu).TableID = TableID;
                TabletopItems[i].Location = loc;
                TabletopItems[i].Rotation = rotation;
                i++;
            }
        }

        public void SubmitSeatRequest(params Person[] people)
        {
            foreach (var p in people)
            {
                p.ParentSpawner.People.Remove(p);
                p.ParentSpawner = null;
                p.Draggable = false;
            }
            Main.Multiplayer.PersonSeatRequest(Player.ControlledCharacter, people[0], ID, Array.IndexOf(OccupiedSeats, true));
        }

        /// <summary>
        /// Only multiplayer client is allowed to call this after the seat request has been accepted.
        /// </summary>
        /// <param name="people"></param>
        public void Seat(int quadrant, params Person[] people)
        {
            int i = 0;
            foreach (var p in people)
            {
                if (p.ParentSpawner != null)
                {
                    p.ParentSpawner.People.Remove(p);
                    p.ParentSpawner = null;
                }
                var quad = quadrant + i;
                if (quad > 3)
                    quad -= 4;
                PositionPerson(p, quad);
                p.Draggable = false;
                i++;
            }
            People = people;
            TableState = States.Menus;
            PositionTableItems();
        }

        public override bool Interact(Player Focus, bool Force = false)
        {
            if (!base.Interact(Focus, Force))
                return false;
            switch (TableState)
            {
                case States.M_Ready:
                    TableState = States.F_Waiting;
                    Focus.PlaceObjectInHand(TabletopItems.Where(x => x is Menu).First());
                    break;
                case States.F_Waiting:
                    if (Focus.Hands.Where(x => x is Food).Any())
                    {
                        var query = Focus.Hands.Where(x => x is Food);
                        if (!query.Any())
                            return false;
                        var food = (Food)query.First().Value;
                        if (food.TableID == TableID) //Served the right table
                        {
                            Focus.RemoveObjectFromHand(food);
                            TableState = States.Eating;
                        }
                    }
                    break;
            }
            Interacting = false;
            return true;
        }

        private bool PositionDraggedPeople()
        {
            if (Occupied)
                return false;
            OccupiedSeats = new bool[4];
            var p = Main.MousePosition;
            var person = Main.ObjectDragging as Person;
            bool right = false, bottom = false;
            if (p.X > BoundingRectangle.Center.X) //Mouse in Right Half
                right = true;
            if (p.Y > BoundingRectangle.Center.Y) //Mouse in Bottom Half
                bottom = true;
            var i = 0;
            if (right == true)
                i = 1;
            else if (bottom)
                i = 2;
            if (right && bottom)            
                i = 3;
            return PositionPerson(person, i);            
        }

        private bool PositionPerson(Person person, int quadrant)
        {
            if (Occupied)
                return false;
            bool right = false, bottom = false;
            var i = quadrant;
            if (i == 1)
                right = true;
            else if (i == 2)
                bottom = true;
            else if (i == 3)
            {
                right = true;
                bottom = true;
            }
            var incH = Height / 4;
            if (!bottom) //Top Half of Table           
                person.Y = (Location.Y + incH) - person.Height;
            else
                person.Y = (Location.Y + (incH * 3)) - person.Height;
            if (right)
            {
                person.X = BoundingRectangle.Right - person.Width;
                person.Effects = SpriteEffects.None;
            }
            else
            {
                person.X = X;
                person.Effects = SpriteEffects.FlipHorizontally;
            }
            if (!right && !bottom) //TopLeft
                OccupiedSeats[0] = true;
            else if (right && !bottom) //TopRight
                OccupiedSeats[1] = true;
            else if (!right && bottom) //BottomLeft
                OccupiedSeats[2] = true;
            else if (right && bottom)
                OccupiedSeats[3] = true;
            return true;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {            
            //Seats need to be drawn underneathe tabletop at LayerDepth
            spriteBatch.Draw(Tableseat, BoundingRectangle, null, BonusColor, Rotation, RotateOrigin, Effects, LayerDepth - .05f);
            base.Draw(spriteBatch);               
            foreach (var i in TabletopItems.Where(x => x != null))            
                i.Draw(spriteBatch);                             
        }
    }

    public class Menu : GameObject
    {
        public int TableID
        {
            get;
            set;
        }
        public Menu(int TableID, string Name = "Default Menu", ObjectNameTable Type = ObjectNameTable.Menu) : base(Name,Type)
        {
            this.TableID = TableID;
            Scale = .60f;
        }
        public override void Load(ContentManager Content)
        {
            if (Texture == null)
                Texture = Content.Load<Texture2D>("Objects/Table/PART_Menu");
            base.Load(Content);
        }
        public virtual bool GiveMeToPlayer()
        {
            return Player.ControlledCharacter.PlaceObjectInHand(this);           
        }
    }

    public class Food : Menu
    {
        public Food(int TableID, string Name = "Default Food Item") : base(TableID, Name, ObjectNameTable.Food)
        {            
            Scale = .60f;
        }
        public override void Load(ContentManager Content)
        {
            Texture = Content.Load<Texture2D>("Objects/Table/PART_Food");
            base.Load(Content);
        }
    }
}
