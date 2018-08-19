using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Diner_Smash
{
    public class WelcomeMat : GameObject
    {
        /// <summary>
        /// The time in seconds between person spawns.
        /// </summary>
        public double Interval = 5;
        /// <summary>
        /// The time in seconds since the last person spawned.
        /// </summary>
        public double TimeSinceLastPerson = 0;
        public List<Person> People = new List<Person>();
        ContentManager Content;

        public WelcomeMat(string Name) : base(Name, ObjectNameTable.WelcomeMat)
        {
            IsInteractable = false;
            IsObjectFlat = true;
        }

        public override void Load(ContentManager Content)
        {
            Texture = Content.Load<Texture2D>("Objects/WelcomeMat");
            this.Content = Content;
            base.Load(Content);
        }

        public override void Update(GameTime gameTime)
        {
            TimeSinceLastPerson += gameTime.ElapsedGameTime.TotalSeconds;
            if (TimeSinceLastPerson >= Interval)
            {
                if (People.Count == 5)
                    goto skip;
                var p = (Person)Create("debugPerson", ObjectNameTable.Person, Content);
                p.ParentSpawner = this;
                Main.AddObject(p);
                People.Add(p);
                TimeSinceLastPerson = 0;
            }
            skip:
            int i = 0;
            foreach(var p in People)
            {
                if (p.IsDragging)
                    break;
                var x = (Location.X + Width / 2) - (p.Width / 2);
                var y = (BoundingRectangle.Center).Y - p.Height - (100 * i);
                p.Location = new Vector2(x, y);
                p.Effects = SpriteEffects.None;
                i++;
            }
            base.Update(gameTime);
        }
    }
}
