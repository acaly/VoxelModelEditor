using LightDx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VoxelModelEditor.Transforms
{
    /*
     * right drag+alt   keep origin, change yaw/pitch and move target
     * right drag       keep target change yaw/pitch and move origin
     * mid drag	        keep offset, move to camera X and camera Y
     * right wasd       keep offset, move to camera X and Z
     * wheel            keep target change length and origin
     * whell+alt        keep length move to carmera Z
     */
    //Note that this camera class only works in single-threaded rendering (can only be accessed from WinForm thread).
    class Camera : IViewTransform
    {
        public Vector3 Target;

        private float _yaw;
        public float Yaw
        {
            get => _yaw;
            set
            {
                if (_yaw != value)
                {
                    _yaw = value;
                    RecalcOffset();
                }
            }
        }

        private float _pitch;
        public float Pitch
        {
            get => _pitch;
            set
            {
                if (_pitch != value)
                {
                    _pitch = Math.Max(Math.Min(value, (float)Math.PI / 2.001f), (float)Math.PI / -2.001f);
                    RecalcOffset();
                }
            }
        }

        private float _length;
        public float Length
        {
            get => _length;
            set
            {
                if (_length != value)
                {
                    _length = value;
                    RecalcOffset();
                }
            }
        }

        private LightDevice _device;
        private Matrix4x4 _projMatrix;

        private Vector3 _offset, _cameraX, _cameraY, _cameraZ; //Note that xyz is left-handed.
        private Matrix4x4 _yawMatrix, _pitchMatrix;

        private Dictionary<Keys, bool> keys;
        private int _drag, _lastX, _lastY;
        private bool _alt;

        public Camera(LightDevice device, Vector3 target)
        {
            _device = device;

            Target = target;
            Length = 10; //trigger initalization

            device.ResolutionChanged += Device_ResolutionChanged;
            Device_ResolutionChanged(null, EventArgs.Empty);
        }

        private void Device_ResolutionChanged(object sender, EventArgs e)
        {
            _projMatrix = _device.CreatePerspectiveFieldOfView((float)Math.PI / 4).Transpose();
        }

        private void RecalcOffset()
        {
            _yawMatrix = Matrix4x4.CreateRotationZ(Yaw);

            _cameraX = Vector3.Transform(new Vector3(0, 1, 0), _yawMatrix);
            _pitchMatrix = Matrix4x4.CreateFromAxisAngle(_cameraX, -Pitch);

            var frontHorizontal = Vector3.Transform(new Vector3(-1, 0, 0), _yawMatrix);
            _cameraZ = Vector3.Transform(frontHorizontal, _pitchMatrix);
            _offset = _cameraZ * Length;

            _cameraY = Vector3.Cross(_cameraX, _cameraZ);
        }

        public Matrix4x4 GetViewMatrix()
        {
            return MatrixHelper.CreateLookAt(Target - _offset, Target, Vector3.UnitZ).Transpose();
        }

        public Matrix4x4 GetViewProjectionMatrix()
        {
            return _projMatrix * GetViewMatrix();
        }

        public void SetForm(Form form)
        {
            form.KeyPreview = true;
            keys = new Dictionary<Keys, bool>() {
                        { Keys.W, false }, { Keys.S, false }, { Keys.A, false }, { Keys.D, false },
                    };
            form.KeyDown += delegate (object obj, KeyEventArgs e)
            {
                if (keys.ContainsKey(e.KeyCode)) keys[e.KeyCode] = true;
            };
            form.KeyUp += delegate (object obj, KeyEventArgs e)
            {
                if (keys.ContainsKey(e.KeyCode)) keys[e.KeyCode] = false;
            };
            form.MouseDown += delegate (object obj, MouseEventArgs e)
            {
                if (_drag != 0) return;
                _drag = (int)e.Button;
                _alt = Control.ModifierKeys == Keys.Alt;
                _lastX = e.X;
                _lastY = e.Y;
            };
            form.MouseUp += delegate (object obj, MouseEventArgs e)
            {
                if ((int)e.Button == _drag) _drag = 0;
            };
            form.MouseMove += delegate (object obj, MouseEventArgs e)
            {
                if (_drag == 0) return;
                var dx = e.X - _lastX;
                var dy = e.Y - _lastY;
                switch (_drag)
                {
                    case (int)MouseButtons.Right:
                        if (_alt)
                        {
                            var oldOffset = _offset;
                            Pitch -= dy * 0.005f;
                            Yaw -= dx * 0.005f;
                            Target += _offset - oldOffset;
                        }
                        else
                        {
                            Pitch += dy * 0.005f;
                            Yaw += dx * 0.005f;
                        }
                        break;
                    case (int)MouseButtons.Middle:
                        Target += (_cameraX * dx + _cameraY * dy) * Length * 0.002f;
                        break;
                }
                _lastX = e.X;
                _lastY = e.Y;
            };
            form.MouseWheel += delegate (object obj, MouseEventArgs e)
            {
                if (Control.ModifierKeys == Keys.Alt)
                {
                    Target += _cameraZ * Math.Sign(e.Delta) * Length * 0.1f;
                }
                else
                {
                    if (e.Delta < 0)
                    {
                        Length *= 1.1f;
                    }
                    else if (e.Delta > 0)
                    {
                        Length /= 1.1f;
                    }
                }
            };
        }

        public void Step()
        {
            if (_drag == (int)MouseButtons.Right)
            {
                int dx = 0, dy = 0;
                if (keys[Keys.A]) dx += 1;
                if (keys[Keys.D]) dx -= 1;
                if (keys[Keys.W]) dy += 1;
                if (keys[Keys.S]) dy -= 1;
                Target += (dx * _cameraX + dy * _cameraZ) * Length * 0.01f;
            }
        }

        public void LookAtBox(Vector3 min, Vector3 max)
        {
            var size = max - min;
            var maxSize = Math.Max(Math.Max(size.X, size.Y), size.Z);
            Target = (max + min) / 2;
            Length = Math.Max(maxSize * 3, 1);
        }

        public Vector2 WorldPosToControl(Vector3 pos)
        {
            var transformed = Vector4.Transform(new Vector4(pos, 1), GetViewProjectionMatrix().Transpose());
            transformed /= transformed.W;
            var x = (transformed.X / 2 + 0.5f) * _device.ScreenWidth;
            var y = (0.5f - transformed.Y / 2) * _device.ScreenHeight;
            return new Vector2(x, y);
        }

        public Vector2 WorldPosToNormalized(Vector3 pos)
        {
            var transformed = Vector4.Transform(new Vector4(pos, 1), GetViewProjectionMatrix().Transpose());
            transformed /= transformed.W;
            return new Vector2(transformed.X, transformed.Y);
        }

        public Ray ControlPosToRay(Vector2 pos)
        {
            throw new NotImplementedException();
        }
    }
}
