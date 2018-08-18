using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diner_Smash
{
    public class Camera
    {
        protected float _zoom; // Camera Zoom
        protected float _rotation; //Camera Rotation
        public Matrix _transform; // Matrix Transform
        public Vector2 _pos; // Camera Position

        public Camera(GraphicsDevice graphicsDevice)
        {
            _zoom = 1.0f;
            _rotation = 0.0f;
            _pos = Vector2.Zero;
            Camera_Viewport = new Rectangle(0,0, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
        }

        public float Zoom
        {
            get { return _zoom; }
            set { _zoom = value; if (_zoom < 0.1f) _zoom = 0.1f; } // Negative zoom will flip image
        }

        /// <summary>
        /// The position to center the camera at.
        /// </summary>
        public Vector2 DesiredPosition
        {
            get { return _pos; }
            set { _pos = value; }
        }

        /// <summary>
        /// The camera's viewport -- NOTE: Rectangles use int values meaning it's not 100% accurate!
        /// </summary>
        public Rectangle Camera_Viewport;

        int _mouseLastScroll = 0;

        public Matrix Transform()
        {
            var center = new Point(Camera_Viewport.Width / 2, Camera_Viewport.Height / 2);
            _transform =
              Matrix.CreateTranslation(new Vector3(-_pos.X, -_pos.Y, 0)) *
                                         Matrix.CreateRotationZ(0) *
                                         Matrix.CreateScale(new Vector3(Zoom, Zoom, 0));
                                         // * Matrix.CreateTranslation(new Vector3(center.X, center.Y, 0));            
            Camera_Viewport.Location = _pos.ToPoint();
            return _transform;
        }

        public bool UnlockedCamera
        {
            get
            {
                return Zoom != 1;
            }
        }

        public Vector2 CalculatedMousePos;
        Point _lastMousePos = Point.Zero;

        public void Update()
        {
            var pos = Mouse.GetState().Position;
            var zChanged = _mouseLastScroll != Mouse.GetState().ScrollWheelValue;
            Zoom += (float)(Mouse.GetState().ScrollWheelValue - _mouseLastScroll) / 1000;
            _mouseLastScroll = Mouse.GetState().ScrollWheelValue;
            if (Mouse.GetState().RightButton == ButtonState.Pressed)            
                DesiredPosition -= (pos - _lastMousePos).ToVector2() / new Vector2(Zoom);
            if (Mouse.GetState().MiddleButton == ButtonState.Pressed && UnlockedCamera)
            {
                Zoom = 1;
            }            
            var pt_ = (Mouse.GetState().Position).ToVector2();
            pt_ /= new Vector2(Zoom);
            CalculatedMousePos = pt_ + Camera_Viewport.Location.ToVector2();
            _lastMousePos = pos;            
        }
    }
}
