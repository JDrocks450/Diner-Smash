using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Diner_Smash
{
    public class Lighting
    {        
        public static Color LightColor = Color.White;
        public static Vector2 LightOrigin = Vector2.Zero;
        public static Color ShadowColor = Color.Black * .5f;
        public static float LightIntensity = 0f;
        static bool ShadowsEnabled
        {
            get => Properties.DinerSmash.Default.EnableShadows;
        }

        public Texture2D ShadowTexture;

        public void GenerateShadow(Texture2D Texture)
        {
            ShadowTexture = GetShadowFromTexture(Texture);
        }        

        public static Texture2D GetShadowFromTexture(Texture2D Input)
        {
            if (!ShadowsEnabled)
                return null;
            var result = new Color[Input.Width * Input.Height];
            Input.GetData(result);
            for(int i = 0; i < result.Length; i++)
            {
                if (result[i] == Color.Transparent)
                    continue;
                result[i] = ShadowColor;
            }
            var r = new Texture2D(Input.GraphicsDevice, Input.Width, Input.Height);
            r.SetData(result);
            return r;
        }

        public static void LoadLightingSettings(XElement SaveElement)
        {
            foreach (var i in typeof(Lighting).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                var v = i.GetValue(null);
                var str = SaveElement.Element(i.Name).Value;
                if (v is Color)
                    i.SetValue(null, new Color(uint.Parse(str)));
                if (v is Vector2)
                    i.SetValue(null, str.ToVector2());
                if (v is float)
                    i.SetValue(null, float.Parse(str));
            }
        }

        public static void SaveLightingSettings(ref XElement SaveElement)
        {
            foreach(var i in typeof(Lighting).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                var v = i.GetValue(typeof(Lighting));
                string returnVal = v.ToString();
                if (v is Color)
                    returnVal = ((Color)v).PackedValue.ToString();
                SaveElement.Add(new XElement(i.Name, returnVal));
            }
        }

        /// <summary>
        /// Positions and draws the ShadowTexture using LightingOrigin
        /// </summary>
        /// <param name="batch"></param>
        public void CastShadow(SpriteBatch batch, Vector2 ObjectPosition, double Scale)
        {
            if (ShadowTexture is null)
                return;
            if (!ShadowsEnabled)
                return;
            var delta = ObjectPosition - LightOrigin;
            var offset = 100 / delta.Length();
            var newPos = ObjectPosition + (delta * offset);
            batch.Draw(ShadowTexture,
                new Rectangle(newPos.ToPoint(),
                (new Vector2(ShadowTexture.Width, ShadowTexture.Height) * (float)Scale).ToPoint()), null,
                Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, (float)ReservedZIndicies.Shadows/100);
        }
    }
}
