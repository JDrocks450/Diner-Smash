using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
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

        public float Zoom
        {
            get { return _zoom; }
            set { _zoom = value; if (_zoom < 0.1f) _zoom = 0.1f; } // Negative zoom will flip image
        }

        /// <summary>
        /// The position to center the camera on.
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

        /// <summary>
        /// Creates a matrix for the spritebatch that automatically focuses on the "Focus" if there is one set.
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <returns></returns>
        public Matrix Transform(GraphicsDevice graphicsDevice)
        {
            var center = new Point(graphicsDevice.Viewport.Width / 2, graphicsDevice.Viewport.Height / 2);
            _transform =       // Thanks to o KB o for this solution
              Matrix.CreateTranslation(new Vector3(-_pos.X, -_pos.Y, 0)) *
                                         Matrix.CreateRotationZ(0) *
                                         Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
                                         Matrix.CreateTranslation(new Vector3(center.X, center.Y, 0));
            Camera_Viewport = new Rectangle((int)(_pos.X - center.X), (int)_pos.Y - center.Y, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);
            return _transform;
        }

        public bool UnlockedCamera
        {
            get
            {
                return Zoom != 1;
            }
        }

        Vector2 resetCamPos;
        Point _lastMousePos = Point.Zero;
        public void Update()
        {
            var pos = Mouse.GetState().Position;
            var zChanged = _mouseLastScroll != Mouse.GetState().ScrollWheelValue;
            Zoom += (float)(Mouse.GetState().ScrollWheelValue - _mouseLastScroll) / 1000;
            _mouseLastScroll = Mouse.GetState().ScrollWheelValue;
            if (UnlockedCamera && zChanged)
                resetCamPos = DesiredPosition;
            if (Mouse.GetState().RightButton == ButtonState.Pressed)            
                DesiredPosition -= (pos - _lastMousePos).ToVector2() / new Vector2(Zoom);
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && UnlockedCamera)
            {
                DesiredPosition = resetCamPos;
                Zoom = 1;
            }
            _lastMousePos = pos;            
        }

        public Camera()
        {
            _zoom = 1.0f;
            _rotation = 0.0f;
            _pos = Vector2.Zero;
        }
    }
}
