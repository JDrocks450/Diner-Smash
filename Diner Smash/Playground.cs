﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Diner_Smash.Extentions;
using System.Xml.Linq;

namespace Diner_Smash
{
    public class Playground
    {        
        public LevelSave Source
        {
            get;
            private set;
        }

        public Texture2D Floor;

        public Playground()
        {

        }

        public List<Point> Floors = new List<Point>();

        public void Setup(ContentManager Content, LevelSave Source)
        {
            Floors.Clear();
            Main.Objects = Source.LoadedObjects;
            Floor = Content.Load<Texture2D>("Objects/Floor");
            for(int i = 0; i < Source.LevelSize.X; i += Floor.Width)
            {
                for(int h = 0; h < Source.LevelSize.Y; h += Floor.Height)
                {
                    Floors.Add(new Point(i, h));
                }
            }
            Main.SourceLevel = Source;
        }

        public void Draw(SpriteBatch batch)
        {
            try
            {
                foreach (var f in Floors)
                    batch.Draw(Floor, f.ToVector2(), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
            }
            catch { }
        }
    }
}