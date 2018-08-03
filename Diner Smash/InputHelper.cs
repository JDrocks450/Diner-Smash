﻿using Microsoft.Xna.Framework.Input;
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
            Keys[] finalizekeys = new Keys[0];
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
            if (!kState.GetPressedKeys().Where(x => x != Keys.LeftShift && x != Keys.RightShift).Any() && pressedKeys.Any())
                finalizekeys = pressedKeys;
            pressedKeys = kState.GetPressedKeys().Where(x => x != Keys.LeftShift && x != Keys.RightShift).ToArray();
            if (left || right || finalizekeys.Any())
                UserInput?.Invoke(new InputEventArgs()
                {
                    PressedKeys = finalizekeys,
                    MouseLeftClick = left,
                    MouseRightClick = right
                });
        }
    }
}