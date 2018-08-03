using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diner_Smash
{
    public static class Extentions
    {
        public static List<ObjectContext> Verify(this List<ObjectContext> entry, ObjectContext CheckAgainst)
        {
            foreach (var i in entry.Where(x => x.Width > CheckAgainst.Width))
                i.Width = CheckAgainst.Width;
            foreach (var i in entry.Where(x => x.Height > CheckAgainst.Height))
                i.Height = CheckAgainst.Height;
            return entry;
        }

        public static void DrawLine(this SpriteBatch spriteBatch, Point begin, Point end, Color color, int width = 1)
        {
            DrawLine(spriteBatch, begin.ToVector2(), end.ToVector2(), color, width);
        }

        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 begin, Vector2 end, Color color, int width = 1)
        {
            Rectangle r = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + width, width);
            Vector2 v = Vector2.Normalize(begin - end);
            float angle = (float)Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
            if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
            spriteBatch.Draw(Main.BaseTexture, r, null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        public static bool Intersects(this Rectangle r, Point p1, Point p2)
        {
            return LineIntersectsLine(p1, p2, new Point(r.X, r.Y), new Point(r.X + r.Width, r.Y)) ||
                   LineIntersectsLine(p1, p2, new Point(r.X + r.Width, r.Y), new Point(r.X + r.Width, r.Y + r.Height)) ||
                   LineIntersectsLine(p1, p2, new Point(r.X + r.Width, r.Y + r.Height), new Point(r.X, r.Y + r.Height)) ||
                   LineIntersectsLine(p1, p2, new Point(r.X, r.Y + r.Height), new Point(r.X, r.Y)) ||
                   (r.Contains(p1) && r.Contains(p2));
        }

        private static bool LineIntersectsLine(Point l1p1, Point l1p2, Point l2p1, Point l2p2)
        {
            float q = (l1p1.Y - l2p1.Y) * (l2p2.X - l2p1.X) - (l1p1.X - l2p1.X) * (l2p2.Y - l2p1.Y);
            float d = (l1p2.X - l1p1.X) * (l2p2.Y - l2p1.Y) - (l1p2.Y - l1p1.Y) * (l2p2.X - l2p1.X);

            if (d == 0)
            {
                return false;
            }

            float r = q / d;

            q = (l1p1.Y - l2p1.Y) * (l1p2.X - l1p1.X) - (l1p1.X - l2p1.X) * (l1p2.Y - l1p1.Y);
            float s = q / d;

            if (r < 0 || r > 1 || s < 0 || s > 1)
            {
                return false;
            }

            return true;
        }

        public static Point Parse(this Point entry, string input)
        {
            if (!input.Contains("{") || !input.Contains("X:") || !input.Contains("Y:"))
                throw new Exception("Incorrect Format!");
            var value = new Point();
            var x = input.Substring(input.IndexOf("X:") + 2, input.IndexOf(' ') - (input.IndexOf("X:") + 2));
            var y = input.Substring(input.IndexOf("Y:") + 2, input.IndexOf('}') - (input.IndexOf("Y:") + 2));
            value.X = int.Parse(x);
            value.Y = int.Parse(y);
            return value;
        }
    }
}
