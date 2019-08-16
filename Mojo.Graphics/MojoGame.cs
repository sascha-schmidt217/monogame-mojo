using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Mojo.Graphics
{
    public class MojoGame : Game
    {
        public Canvas Canvas { get; private set; }
        public GraphicsDeviceManager DeviceManager { get; private set; }

        public bool LightingEnabled { get; set; } = false;
        public int  Width { get; private set; }
        public int Height { get; private set; }

        public MojoGame(int width, int height, bool fullScreen)
        {
            DeviceManager = new GraphicsDeviceManager(this);
            DeviceManager.PreferredBackBufferWidth = width;
            DeviceManager.PreferredBackBufferHeight = height;
            DeviceManager.IsFullScreen = fullScreen;

            Global.Initialize(this);

            Width = width;
            Height = height;
        }
        protected override void LoadContent()
        {
            Global.LoadContent();
            Canvas = new Canvas();
        }
    }
}
