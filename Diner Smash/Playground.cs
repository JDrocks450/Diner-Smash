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
        public Texture2D Floor;
        public Color FloorMask = Color.White;

        int column = 0;
        int row = 0;

        public Playground()
        {
            
        }

        public List<Point> Floors = new List<Point>();

        public void Setup(ContentManager Content, LevelSave Source)
        {
            if (Source is null)
                return;
            Floors.Clear();
            Main.Objects = Source.LoadedObjects;
            Floor = Content.Load<Texture2D>("Floor");
            SetFlooring(Source);            
        }

        public void SetFlooring(LevelSave Source)
        {
            for (int i = 0; i < Source.LevelSize.X; i += Floor.Width)
            {
                for (int h = 0; h < Source.LevelSize.Y; h += Floor.Height)
                {
                    Floors.Add(new Point(i, h));
                    column = h;
                }
                row = i;
            }
            column+=Floor.Height;
            row+=Floor.Width;
            Source.LevelSize = new Point(row, column);
        }

        public void Draw(SpriteBatch batch)
        {
            int padding = -5;
            batch.Draw(Main.BaseTexture,
                new Rectangle(new Point(padding), new Point(row+padding*2*-1, column+padding*2 * -1)),
                Color.Red);
            try
            {
                foreach (var f in Floors)
                    batch.Draw(Floor, f.ToVector2(), null, Color.Lerp(FloorMask,Lighting.LightColor, Lighting.LightIntensity), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
            catch { }
        }        
    }
}
