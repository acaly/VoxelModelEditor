using LightDx;
using LightDx.InputAttributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VoxelModelEditor.Document.Models;
using VoxelModelEditor.Document.Postures;
using VoxelModelEditor.Transforms;

namespace VoxelModelEditor.Windows
{
    partial class DisplayForm : Form, IVoxelVertexBufferFactory
    {
        private struct GSConstant
        {
            public uint EdgeIndex;
        }

        private Form _renderWindow;
        private bool _renderWindowInResize;
        private int _renderWindowLastWidth, _renderWindowLastHeight;

        private LightDevice _device;
        private RenderTargetList _target;
        private Pipeline _modelPipeline;
        private VertexDataProcessor<Voxel> _inputProcessor;
        private ConstantBuffer<Matrix4x4> _vsConstant;
        private ConstantBuffer<GSConstant> _gsConstant;

        private Sprite _spriteDebug;
        private TextureFontCache _spriteFont;

        private Camera _camera;

        public EditableModel Model { get; private set; }
        public IPostureSource PostureSource { get; set; }

        public DisplayForm()
        {
            InitializeComponent();
            ClientSize = new Size(800, 600);
            InitializeRendering();
            Model = new EditableModel(this);
        }

        private void InitializeRendering()
        {
            _renderWindow = new Form();
            _renderWindow.FormBorderStyle = FormBorderStyle.None;
            _renderWindow.TopLevel = false;
            _renderWindow.Parent = this;
            _renderWindow.Dock = DockStyle.Fill;
            _renderWindow.Show();
            
            _device = LightDevice.Create(_renderWindow);
            _device.AutoResize = false; //We don't use loop, so AutoResize is not checked.

            _spriteDebug = new Sprite(_device);
            _spriteFont = new TextureFontCache(_device, SystemFonts.DefaultFont);

            _target = new RenderTargetList(_device.GetDefaultTarget(Color.AliceBlue.WithAlpha(1)), _device.CreateDepthStencilTarget());
            _target.Apply();

            _modelPipeline = _device.CompilePipeline(InputTopology.Point,
                ShaderSource.FromResource("Model.fx", ShaderType.Vertex | ShaderType.Geometry | ShaderType.Pixel));
            _inputProcessor = _modelPipeline.CreateVertexDataProcessor<Voxel>();

            _vsConstant = _modelPipeline.CreateConstantBuffer<Matrix4x4>();
            _modelPipeline.SetConstant(ShaderType.Vertex, 0, _vsConstant);
            _gsConstant = _modelPipeline.CreateConstantBuffer<GSConstant>();
            _modelPipeline.SetConstant(ShaderType.Geometry, 0, _gsConstant);

            _camera = new Camera(_device, new Vector3(0, 0, 0));
            _camera.SetForm(_renderWindow);

            _renderWindowLastWidth = _renderWindow.ClientSize.Width;
            _renderWindowLastHeight = _renderWindow.ClientSize.Height;
            ResizeBegin += RenderWindow_ResizeBegin;
            ResizeEnd += RenderWindow_ResizeEnd;
            _renderWindow.ResizeBegin += RenderWindow_ResizeBegin;
            _renderWindow.ResizeEnd += RenderWindow_ResizeEnd;
            _renderWindow.ClientSizeChanged += RenderWindow_ClientSizeChanged;

            _renderWindow.DoubleClick += delegate (object obj, EventArgs e)
            {
                ResetCamera();
            };
        }

        private void DisplayForm_Load(object sender, EventArgs e)
        {
            ResetCamera();
        }

        private void ResetCamera()
        {
            Model.CalculateBound(out var min, out var max);
            _camera.LookAtBox(min, max);
        }

        private void RenderWindow_ResizeEnd(object sender, EventArgs e)
        {
            _renderWindowInResize = false;
            RenderWindow_ClientSizeChanged(null, EventArgs.Empty);
        }

        private void RenderWindow_ResizeBegin(object sender, EventArgs e)
        {
            _renderWindowInResize = true;
        }

        private void RenderWindow_ClientSizeChanged(object sender, EventArgs e)
        {
            if (_renderWindowInResize || _renderWindow.WindowState == FormWindowState.Minimized)
            {
                return;
            }
            var newSize = _renderWindow.ClientSize;
            if (newSize.Width != _renderWindowLastWidth || newSize.Height != _renderWindowLastHeight)
            {
                _device.ChangeResolution(newSize.Width, newSize.Height);
            }
        }

        private void Render()
        {
            _target.ClearAll();

            _modelPipeline.Apply();
            _camera.Step();
            _vsConstant.Value = _camera.GetViewProjectionMatrix();
            _vsConstant.Update();

            for (int pass = 0; pass < 5; ++pass)
            {
                _gsConstant.Value.EdgeIndex = (uint)pass;
                _gsConstant.Update();
                for (int i = 0; i < Model.PartCount; ++i)
                {
                    Model.GetPart(i).DrawAll();
                }
            }

            {
                var point2D = _camera.WorldPosToControl(new Vector3(-20, -25, 45));
                _spriteDebug.Apply();
                _spriteDebug.DrawString(_spriteFont, "X", point2D.X, point2D.Y, 1000);
            }

            _device.Present(true);
        }

        private void TimerRender_Tick(object sender, EventArgs e)
        {
            Render();
        }

        public VertexBuffer CreateDynamic(int size)
        {
            return _inputProcessor.CreateDynamicBuffer(size);
        }

        public VertexBuffer CreateStatic(Voxel[] data)
        {
            return _inputProcessor.CreateImmutableBuffer(data);
        }
    }
}
