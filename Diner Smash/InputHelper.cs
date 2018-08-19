using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diner_Smash
{
    public class InputHelper
    {
        public struct InputEventArgs
        {
            public Keys[] PressedKeys;
            public bool MouseLeftClick;
            public bool MouseRightClick;
        }
        public delegate void UserInputHandler(InputEventArgs e);
        public event UserInputHandler UserInput;

        bool _mouseLeftDown = false;
        bool _mouseRightDown = false;
        Keys[] pressedKeys = new Keys[0];

        public void Listen()
        {
            bool left = false, right = false;
            List<Keys> finalizekeys = new List<Keys>();
            var mState = Mouse.GetState();
            if (mState.LeftButton == ButtonState.Pressed)
                _mouseLeftDown = true;
            else if (_mouseLeftDown)
            {
                _mouseLeftDown = false;
                left = true;
            }
            if (mState.RightButton == ButtonState.Pressed)
                _mouseRightDown = true;
            else if (_mouseRightDown)
            {
                _mouseRightDown = false;
                right = true;
            }
            var kState = Keyboard.GetState();
            var nowPressed = kState.GetPressedKeys();
            foreach (var key in nowPressed)
                if (!pressedKeys.Contains(key))
                    finalizekeys.Add(key);
            pressedKeys = nowPressed.ToArray();
            if (left || right || finalizekeys.Any())
                UserInput?.Invoke(new InputEventArgs()
                {
                    PressedKeys = finalizekeys.ToArray(),
                    MouseLeftClick = left,
                    MouseRightClick = right
                });
        }

        public GameObject CollisionCheck(MouseState mouse, List<GameObject> CheckThrough)
        {
            try
            {
                var results = new List<GameObject>();
                foreach (var x in CheckThrough)
                {
                    x.IsMouseOver = false;
                    var MouseRect = new Rectangle(Main.MousePosition.ToPoint(), new Point(1, 1));
                    if (x.BoundingRectangle.Intersects(MouseRect)) //Per-Pixel detection (fast)
                    {                                              
                        var pt = Main.MousePosition - x.BoundingRectangle.Location.ToVector2();
                        if (x.Scale != 1)
                            pt /= new Vector2((float)x.Scale);
                        x.ScaledCollisionMousePosition = pt;                        
                        var data = new Color[1];
                        x.Texture.GetData(0, new Rectangle(pt.ToPoint(), new Point(1, 1)), data, 0, 1);
                        if (data[0] != Color.Transparent)
                            x.IsMouseOver = true;
                        if (x.IsMouseOver)
                            results.Add(x);
                    }
                }            
            if (results.Any())
            {
                var result = results.Where(x => x.LayerDepth == results.Select(d => d.LayerDepth).Max()).Last(); //Drawing is currently IMMEDIATE meaning the last object is on-top
                foreach (var r in results.Where(x => x != result))
                {
                    r.IsMouseOver = false;
                }
                if (mouse.LeftButton == ButtonState.Pressed)
                    result.Click();
                return result;
            }
            }
            catch (Exception) { return null; }
            return null;
        }
    }
}
