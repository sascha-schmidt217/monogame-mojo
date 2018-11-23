using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Mojo.Graphics;

namespace Example
{
    public class Game1 : MojoGame
    {
        private const float RAD_TO_DEG = 57.2957795130823208767981548141052f;

        private Image _floor;
        private Image _logo;
        private Image _logo2;
        private float _viewRot;
        private float _lightRot;

        public Game1() : base(640,480,false)
        {
            Content.RootDirectory = "Content";
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            base.LightingEnabled = true;

            _floor = new Image("Images/tiles", "Images/tiles_n" , "Images/tiles_s");
            _floor.Specularity = 1;
            _logo = new Image("Images/logo", "Images/logo_n", 24,24);
            _logo.Specularity = 1;
            _logo2 = new Image("Images/logo2", "Images/logo2_n", 24,24);
            _logo2.Specularity = 1;
            _logo.ShadowCaster = new ShadowCaster(_logo.Width / 2, 24);
            _logo2.ShadowCaster = new ShadowCaster(new Rectangle(-_logo2.Width/2, -_logo2.Height/2, _logo2.Width, _logo2.Height));
        }

        private float _lightDepth = 96.0f;
        private bool _spaceHit = true;
        private bool _enterHit = true;
        private ShadowType _shadowType = ShadowType.Illuminated;

        protected override void Update(GameTime gameTime)
        {
            var state = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || state.IsKeyDown(Keys.Escape))
                Exit();

            if (state.IsKeyDown(Keys.Left))
                _lightDepth = MathHelper.Max(0, _lightDepth - 0.2f);

            if (state.IsKeyDown(Keys.Right))
                _lightDepth = MathHelper.Min(256, _lightDepth + 0.2f);

            if (_spaceHit && state.IsKeyDown(Keys.Space))
            {
                Canvas.ShowGBuffer = !Canvas.ShowGBuffer;
                _spaceHit = false;
            }
            else if(!state.IsKeyDown(Keys.Space))
                _spaceHit = true;

            if (_enterHit && state.IsKeyDown(Keys.Enter))
            {
                Canvas.ShadowEnabled = !Canvas.ShadowEnabled;
                _enterHit = false;
            }
            else if (!state.IsKeyDown(Keys.Enter))
                _enterHit = true;


            if(state.IsKeyDown(Keys.F1))
                _shadowType = ShadowType.Illuminated;
            else if (state.IsKeyDown(Keys.F2))
                _shadowType = ShadowType.Occluded;
            else if (state.IsKeyDown(Keys.F3))
                _shadowType = ShadowType.Solid;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var mouse = Mouse.GetState();
          
            _viewRot += 0.004f;
            _lightRot += 0.02f;

            Canvas.BeginLighting();

            // Set Ambient color used for lighting
            Canvas.TextureFilteringEnabled = true;
            Canvas.AmbientColor = new Color(48,48,48);
            Canvas.Clear(Color.Red);

            // Draw background
            Canvas.Color = Color.White;
            for (int x = -_floor.Width; x < Width + _floor.Width; x += _floor.Width)
            {
                for (int y = -_floor.Height; y < Height + _floor.Height; y += _floor.Height)
                {
                    Canvas.DrawImage(_floor, x, y);
                }
            }

            _logo.ShadowCaster.ShadowType = _shadowType;
            _logo2.ShadowCaster.ShadowType = _shadowType;

            // Draw sprites
            int k = 0;
            for (float an = 0; an < System.Math.PI * 2; an += (float)System.Math.PI * 2 / 8)
            {
                float xx = Width / 2 + (float)System.Math.Cos(an) * 128;
                float yy = Height / 2 + (float)System.Math.Sin(an) * 128;
                Canvas.DrawImage((k++) % 2 == 0 ? _logo : _logo2, xx, yy);
            }

            // add light
            Canvas.AddPointLight( mouse.X, mouse.Y, 512, 1,12, _lightDepth);
          
            Canvas.EndLighting();
            base.Draw(gameTime);
        }
    }
}
